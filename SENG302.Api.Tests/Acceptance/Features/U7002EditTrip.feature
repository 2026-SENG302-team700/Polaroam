Feature: U7002_As Benji I want to edit my trips So that I can correct their information

    Scenario: AC2 - Update a trip with valid information
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with valid information
        Then the changes to the trip are persisted
        And I am transported back to the trip page

    Scenario: AC3 - Edit trip with invalid trip title length
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with an invalid title length
        Then the trip is not updated
        And I see the edit trip error message "Trip title is required and must be between 3 and 128 characters long"
    
    Scenario: AC4 - Edit trip with invalid trip title characters
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with invalid trip title characters
        Then the trip is not updated
        And I see the edit trip error message "Trip title cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers"

    Scenario: AC5 - Edit trip with an empty date
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with an empty start date
        Then the trip is not updated
        And I see the edit trip error message "Invalid date, date must be formatted "

    Scenario: AC6 - Edit trip with end date before start date
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with an end date before the start date
        Then the trip is not updated
        And I see the edit trip error message "End date must not be before the start date"
    
    Scenario: AC7 - Edit trip with too long description
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with a too long description
        Then the trip is not updated
        And I see the edit trip error message "Description must be 2048 characters or less"