Feature: U7001_As Benji I want to be able to create trips So that I have a place to put all my information relating to the trip. about my workouts.

    Scenario: AC2 - Create a new trip
        Given I am on the create trip form page And I enter a valid title, an optional valid description, a valid date range
        When I click 'Create Trip'
        Then the trip is persisted

    Scenario: AC3 - Create a trip with short title
        Given I am on the create trip form page And I enter a short title
        When I click 'Create Trip'
        Then I see the error message 'Trip title must be between 3 and 128 characters long'

    Scenario: AC6 -  Cannot create trip with end date before start date
        Given I am on the create trip form page And I select an end date that is before the selected start date
        When I click 'Create Trip'
        Then the trip is not created, And I see an error message 'End date must not be before the start date' preserved and displayed in the trip description


    Scenario: AC7 - Create trip with long description
        Given I am on the create trip form page And I enter a long description
        When I click 'Create Trip'
        Then I see the error message 'Description must be 2048 characters or less'

