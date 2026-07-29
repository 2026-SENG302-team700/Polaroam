Feature: U7003_As Benji I want to be able to delete trips So that I can remove trips for whatever reasons be they personal or otherwise.
    
    Scenario: AC2 - Delete a trip
        Given I have a trip
        When I delete the trip
        Then the trip no longer exists in the database
