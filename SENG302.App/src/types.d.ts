type PfpData = {
    imageSource: string,
    offsetX: number,
    offsetY: number,
    zoom: number
}

type PostPhotoData = {
    imageSource: string,
}

type NotificationActionButton = {
    function: () => void,
    text: string,
    colour: string
}

type NotificationData = {
    title: string,
    text: string,
    isRead: boolean,
    id: number,
    leftAction?: NotificationActionButton,
    rightAction?: NotificationActionButton,
    readToggle: () => void,
}

type NotificationResponse = {
    id: number,
    readStatus: boolean,
    handled: boolean,
    headerText: string,
    bodyText: string,
    timestamp: string,
    tripId?: number,
    notificationType: string
}

type GetAllNotificationsResponse = {
    notificationResponses: NotificationResponse[],
    unreadCount: number,
    moreExist: boolean,
}

type CollaboratingUser = {
    userId: number,
    displayName: string,
}

type UserResponse = {
    id: number,
    email: string,
    displayName: string,
    country: string,
    profilePicture: number,
    profilePictureOffsetX: number,
    profilePictureOffsetY: number,
    profilePictureZoom: number,
    acceptedAiPrivacyPolicy: boolean,
    aiTagging: boolean
    
}