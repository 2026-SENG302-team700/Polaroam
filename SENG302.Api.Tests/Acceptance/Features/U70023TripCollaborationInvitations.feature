Feature: U70023 - As a user I want to be able to invite polaroam users to collaborate on my trips so that we can both contribute to the same trip together

    Scenario: AC4 - Invite notification
        Given I am a user of Polaroam
        When I am sent a collaboration invite
        Then I receive a notification with options to accept or decline and a email letting me know I have been invited to the specific trip