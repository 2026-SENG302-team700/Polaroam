Feature: U7001_As Benji I want to be able to create trips So that I have a place to put all my information relating to the trip. about my workouts.
    
    Scenario: AC2 - Create a new trip
        Given I am on the create trip form page And I enter a valid title, an optional valid description, a valid date range
        When I click 'Create Trip'
        Then the trip is persisted