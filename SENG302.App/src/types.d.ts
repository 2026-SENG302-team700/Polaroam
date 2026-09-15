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
    isRead: boolean
    leftAction?: NotificationActionButton,
    rightAction?: NotificationActionButton,
    readToggle: () => void,
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