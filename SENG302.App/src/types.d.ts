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

type Trip = {
    id: number,
    title: string,
    userId: number,
    ownerName: string,
    description: string,
    startDate: string,
    endDate: string,
    collaborators: CollaboratingUser[],
}

type TripPhotoGroup = {
    tripId: number,
    tripTitle: string,
    photos: File[],
    photoIds: number[],
}

type TripIdAndPhoto = {
    tripId: number,
    photoId: number,
}

type Post = {
    postId: number,
    tripId: number,
    title: string,
    content: string,
    date: string,
    osmId: number,
    osmType: string,
    locationName: string,
    photoIds: number[],
    creationTime: string,
    creatorId: number,
    creatorDisplayName: string,
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