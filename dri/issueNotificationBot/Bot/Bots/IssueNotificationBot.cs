﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IssueNotificationBot
{
    public class IssueNotificationBot<T> : SignInBot<T> where T : Dialog
    {
        public IssueNotificationBot(ConversationState conversationState, UserState userState, T dialog, ILogger<SignInBot<T>> logger, UserStorage userStorage, NotificationHelper notificatioHelper)
            : base(conversationState, userState, dialog, logger, userStorage, notificatioHelper)
        {
        }

        protected override async Task OnTeamsMembersAddedAsync(IList<TeamsChannelAccount> membersAdded, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"New members added: { string.Join(", ", membersAdded.Select(m => m.Name).ToArray())}");

            if (turnContext.Activity.Conversation.ConversationType != Constants.PersonalConversationType)
            {
                foreach (var member in membersAdded)
                {
                    // Greet each new user when user is added
                    if (member.Id != turnContext.Activity.Recipient.Id && !await UserStorage.HaveUserDetails(member.Id))
                    {
                        await GreetNewTeamMember(member, turnContext, cancellationToken);
                    }
                    // If the bot is added, we need to get all members and message them to login, proactively, if we don't have their information already
                    else
                    {
                        try
                        {
                            var teamMembers = await TeamsInfo.GetTeamMembersAsync(turnContext);
                            foreach (var teamMember in teamMembers)
                            {
                                if (teamMember.Id != turnContext.Activity.Recipient.Id && !await UserStorage.HaveUserDetails(member.Id))
                                {
                                    try
                                    {
                                        await GreetNewTeamMember(teamMember, turnContext, cancellationToken);
                                    }
                                    // Users that block the bot throw Forbidden errors. We'll catch all exceptions in case
                                    // unforseen errors occur; we want to message as many members as possible.
                                    catch (Exception e)
                                    {
                                        Logger.LogError(new EventId(1), e, $"Something went wrong when greeting { member.Name }");
                                    }
                    }
                            }
                        }
                        catch (InvalidOperationException e)
                        {
                            Logger.LogInformation($"Not in a Teams Team:\n{e}");
                        }
                    }
                }
            }
        }

        protected override async Task OnTeamsMembersRemovedAsync(IList<TeamsChannelAccount> teamsMembersRemoved, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"New members removed: { string.Join(", ", teamsMembersRemoved.Select(m => m.Name).ToArray())}");

            if (turnContext.Activity.Conversation.ConversationType != Constants.PersonalConversationType)
            {
                foreach (var member in teamsMembersRemoved)
                {
                    if (member.Id != turnContext.Activity.Recipient.Id)
                    {
                        await UserStorage.RemoveUser(member.Id);
                    }
                }
            }
        }
    }
}
