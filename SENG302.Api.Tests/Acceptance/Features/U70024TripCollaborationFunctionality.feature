Feature: U70024 - As Jeffery I want to be able to collaborate on trips So that we can both contribute to Trips together

    Scenario: AC5 Confirm kick
        Given I am viewing a confirm kick modal
        When I press Confirm
        Then the modal closes, that user is removed as a collaborator and can no longer contribute to the trip
        
    Scenario: AC7 Kicked notification
        Given I am collaborating on a trip
         When I am kicked from the trip
         Then I receive a notification and email letting me know that I was kicked from the specific trip