Feature: U7007_As Benji I want to edit my trip posts So that I can correct or update information about key moments from my trips.

    Scenario: AC2 - Edit Post with all properties
        Given I am on the edit post form page And I enter a valid title, location, date, images and content
        When I click 'Update Post'
        Then the post is updated

    Scenario: AC18 - Edit post with an invalid location
        Given I am editing an existing post and enter location text without selecting a suggested location
        When I click 'Update Post'
        Then I see the edit post error message "This isn't a valid location"
        And the post is not updated
