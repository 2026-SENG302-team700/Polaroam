Feature: U7004_As Benji I want to be able to create posts So that I can log key moments from specific trips.
    
    Scenario: AC2 - Create a new post
        Given I am on the create post form And I enter a valid title, location, date, and optional content
        When I click 'Create Post'
        Then the post is persisted
        
    Scenario: AC4 - Create Post with invalid date
        Given I am on the create post form And I enter a miss-formed or null date
        When I click 'Create Post'
        Then I see the error message 'Invalid date, date must be formatted [INSERT LOCALISED FORMAT]'
        
    Scenario: AC5 - Create Post with date in the future
        Given I am on the create post form And I enter a date in the future
        When I click 'Create Post'
        Then I see an error message 'Invalid date, date cannot be in the future' 