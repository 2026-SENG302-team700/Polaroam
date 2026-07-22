Feature: U7004_As Benji I want to be able to create posts So that I can log key moments from specific trips.
    
    Scenario: AC2 - Create a new post
        Given I am on the create post form And I enter a valid title, location, date, and optional content
        When I click 'Create Post'
        Then the post is persisted