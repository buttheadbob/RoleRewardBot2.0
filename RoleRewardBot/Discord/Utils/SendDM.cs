using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace RoleRewardBot.Discord.Utils
{
    public sealed class SendDM
    {
        public async Task<string> SendDirectMessage(DiscordMember user, string message)
        {
            try
            {
                await user.SendMessageAsync(message);
                return "Message sent successfully.";
            }
            catch (UnauthorizedException error)
            {
                return
                    $"{error} => Unable to send message to {user.DisplayName}, the user has either blocked the bot or not allowed messages from the public.";
            }
            catch (NotFoundException error)
            {
                return $"{error} => Unable to send message, user not found.";
            }
            catch (BadRequestException error)
            {
                return $"{error} => Invalid DM, cannot send message.";
            }
            catch (ServerErrorException error)
            {
                return $"{error} => Discord was unable to process the request.";
            }
            catch (Exception error)
            {
                return $"{error} => Unknown error.";
            }
        }
    }
}