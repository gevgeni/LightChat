export const state = {
    connection: null,
    jwtToken: '',
    currentUserId: null,
    currentUsername: null,
    activeChatId: null,
    isActiveChatDirect: false,

    oldestMessageId: null,
    isLoadingMessages: false,
    hasMoreMessages: true,

    pendingChatToOpen: null
};

export function resetState() {
    state.connection = null;
    state.jwtToken = '';
    state.currentUserId = null;
    state.currentUsername = null;
    state.activeChatId = null;
    state.isActiveChatDirect = false;
    state.oldestMessageId = null;
    state.isLoadingMessages = false;
    state.hasMoreMessages = true;
    state.pendingChatToOpen = null;
}