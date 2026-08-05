Feature: U7004_As Benji I want to be able to create posts So that I can log key moments from specific trips.

    Scenario: AC2 - Create a new post
        Given I am on the create post form And I enter a valid title, location, date, and optional content
        When I click 'Create Post'
        Then the post is persisted

    Scenario: AC3 - Create post with invalid title length
        Given I am on the create post form And I enter a title that is not between 3 and 128 characters
        When I click 'Create Post'
        Then I see an error message 'Title is required and must be between 3 and 128 characters long'

    Scenario: AC4 - Create Post with invalid date
        Given I am on the create post form And I enter a miss-formed or null date
        When I click 'Create Post'
        Then I see an error message 'Date is invalid'

    Scenario: AC5 - Create Post with date in the future
        Given I am on the create post form And I enter a date in the future
        When I click 'Create Post'
        Then I see an error message 'Invalid date, date cannot be in the future'

    Scenario: AC14 - Create post with an invalid location
        Given I enter location text without selecting a suggested location
        When I click 'Create Post'
        Then I see an error message 'This isn't a valid location'