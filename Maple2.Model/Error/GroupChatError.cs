// ReSharper disable InconsistentNaming, IdentifierTypo

using System.ComponentModel;

namespace Maple2.Model.Error;

public enum GroupChatError {
    none = 0,
    [Description("You cannot invite that player to the group chat.")]
    s_err_groupchat_null_target_user = 3,
    [Description("You cannot invite that player to the group chat.")]
    s_err_groupchat_add_member_target = 8,
    [Description("{0} is already participating in 3 group chats.")]
    s_err_groupchat_maxgroup = 10,

    // Unknown values
    [Description("Group chats can include up to 20 people.")]
    s_err_groupchat_maxjoin,
    [Description("You are already participating in the chat.")]
    s_err_groupchat_join_exist,
    [Description("There is a group chat with the same name.")]
    s_err_groupchat_name_exist,
}
