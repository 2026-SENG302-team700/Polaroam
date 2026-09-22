Feature: U70022 - As a user I want to view and manage my notifications so that I can keep track of updates

    Scenario: AC1 - View latest notifications in the correct order
        Given I am logged in and have read and unread notifications
        When I open the notification menu
        Then I see unread notifications before read notifications
        And the notifications in each section are newest first

    Scenario: AC4 - See more notifications
        Given I am logged in and have more than five notifications
        When I open the notification menu
        And I request more notifications
        Then more notifications are loaded

    Scenario: AC6 - Mark notification as read
        Given I am logged in and have an unread notification
        When I mark the notification as read
        Then the notification is marked as read

    Scenario: AC7 - Display unread notification count
        Given I am logged in and have three unread notifications and one read notification
        When I open the notification menu
        Then the unread notification count is three

    Scenario: AC10 - Mark notification as unread
        Given I am logged in and have a read notification
        When I mark the notification as unread
        Then the notification is marked as unread
