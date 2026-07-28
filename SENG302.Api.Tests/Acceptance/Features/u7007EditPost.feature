Feature: U7007_As Benji I want to edit my trip posts So that I can correct or update information about key moments from my trips.

    Scenario: AC2 - Edit Post with all properties
        Given I am on the edit post form page And I enter a valid title, location, date, images and content
        When I click 'Update Post'
        Then the post is updated
        
        