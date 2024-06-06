using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Using the cached proposed blocking changes, commit all the changes using direct access to the api
    /// </summary>
    public class CommitProposedBlocksCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "CommitProposed";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["cp"];

        public string Description => "Commits proposed changes from the current cache (loaded with load command)";

        public string ExtendedDescription => "Should clean up the temp block list and condense all perma blocks into CIDR ranges where possible";

        public CommitProposedBlocksCommand()
            : base(Globals.Logger)
        {

        }

        public ICommand MakeCommand(string[] args)
        {
            return new CommitProposedBlocksCommand();
        }

        public void Run()
        {
            if (!IsConnectionOk(Globals.ApiClient))
                return;

            Log.Info("---- Commiting Propossed IP blocks ----");

            try
            {
                //Find all the IPs that are currently in the IDS block list (temp blocks have a timeout)
                var allTempBlocks = Globals.ApiClient!.GetCurrentlyBlockedIPs().ConfigureAwait(false).GetAwaiter().GetResult();
                if (!IsResponseOk(allTempBlocks))
                    throw new Exception("Unable to access temp block data, aborting");

                //For each CIDR group that was identified, Create a black list entry and then clean up any IPs from the temp and black lists that are now covered by the CIDR
                Log.Info("Setting up CIDR groups");
                foreach (var grp in Cache.ProposedIpGroups)
                {
                    //Create the new group
                    var newGrpResponse = Globals.ApiClient.SavePermaBlockedIP(grp.Subnet, $"[{DateTime.UtcNow.ToShortDateString()}] Created from {grp.BlockedIps.Count} IP blocks").ConfigureAwait(false).GetAwaiter().GetResult();
                    if (IsResponseOk(newGrpResponse))
                    {
                        Log.Info($"Saved Entry for CIDR:{grp.Subnet} to clean up {grp.BlockedIps.Count} IP blocks");
                        foreach (var blk in grp.BlockedIps)
                        {
                            if (blk.IsTemporary)
                            {
                                //remove temp ban
                                var existingTempBlock = allTempBlocks!.ipBlocks?.Where(b => b.ip == blk.Ip).FirstOrDefault();
                                if (existingTempBlock != null)
                                {
                                    var delTempResponse = Globals.ApiClient.DeleteTempBlockedIP(existingTempBlock).ConfigureAwait(false).GetAwaiter().GetResult();
                                    if (IsResponseOk(delTempResponse))
                                        Log.Info($"Cleaned up Temp Block on: {blk.Ip}");
                                    else
                                        Log.Warning($"FAILED: Cleaning up Temp Block on: {blk.Ip} {delTempResponse.message}");
                                }
                                else
                                    Log.Warning($"Unable to Locate existing temp block on {blk.Ip}");
                            }
                            else
                            {
                                //remove perma ban
                                var delPermaResponse = Globals.ApiClient.DeletePermaBlockedIP(blk.Ip).ConfigureAwait(false).GetAwaiter().GetResult();
                                if (IsResponseOk(delPermaResponse))
                                    Log.Info($"Cleaned up Perma Block on: {blk.Ip}");
                                else
                                    Log.Warning($"FAILED: Cleaning up Perma Block on: {blk.Ip} {delPermaResponse.message}");
                            }
                        }
                    }
                    else
                    {
                        Log.Warning($"FAILED: Creating Entry for CIDR:{grp.Subnet} {newGrpResponse.message}");
                    }
                }

                //For all the remaning temporary (IDS timout) blocks, move them to the black list
                Log.Info("Moving remaining temp blocks to perma bans");
                
                var newIpBlocks = Cache.AllBlockedIps           //Select all temp blocks that are not part of a group
                    .Where(b => b.IsTemporary
                             && !Cache.ProposedIpGroups
                                      .SelectMany(g => g.BlockedIps)
                                      .Any(gp => gp.Ip == b.Ip)).ToList();
                
                foreach (var tb in newIpBlocks)
                {
                    var existingTempBlock = allTempBlocks!.ipBlocks?.Where(b => b.ip == tb.Ip).FirstOrDefault();
                    if (existingTempBlock != null)
                    {
                        var delTempResponse = Globals.ApiClient.DeleteTempBlockedIP(existingTempBlock).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (IsResponseOk(delTempResponse))
                        {
                            Log.Info($"Cleaned up Temp Block on: {tb.Ip}");
                            var savePermBlockResponse = Globals.ApiClient.SavePermaBlockedIP(tb.Ip, $"Created[{DateTime.UtcNow.ToShortDateString()}] auto moved from IDS").ConfigureAwait(false).GetAwaiter().GetResult();
                            if (IsResponseOk(savePermBlockResponse))
                                Log.Info($"Added Perma block on: {tb.Ip}");
                            else
                                Log.Warning($"FAILED: Adding Perma block on: {tb.Ip}");
                        }
                        else
                            Log.Warning($"FAILED: Cleaning up Temp Block on: {tb.Ip} {delTempResponse.message}");
                    }
                    else
                        Log.Warning($"Unable to Locate existing temp block on {tb.Ip}");
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Failed to complete commit: {ex.Message}");
            }
            finally
            {
                Log.Info("---- Done Commiting Propossed IP blocks ----");
            }
        }
    }
}
