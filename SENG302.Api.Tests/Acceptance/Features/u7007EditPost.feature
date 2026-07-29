Feature: U7007_As Benji I want to edit my trip posts So that I can correct or update information about key moments from my trips.

    Scenario: AC2 - Edit Post with all properties
        Given I am on the edit post form page And I enter a valid title, location, date, images and content
        When I click 'Update Post'
        Then the post is updated

    Scenario: AC13 - Edit post with an image larger than 10MB
        Given I am editing an existing post with an image larger than 10MB
        When I click 'Update Post'
        Then I see the edit post photo error message "Image too large, maximum file size is 10MB"
        And no new images are associated with the post

    Scenario: AC14 - Edit post with an unsupported image type
        Given I am editing an existing post with an unsupported image type
        When I click 'Update Post'
        Then I see the edit post photo error message "Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp"
        And no new images are associated with the post

    Scenario: AC16 - Edit post and add valid images
        Given I am editing an existing post with two valid images to add
        When I click 'Update Post'
        Then the post is updated
        And the two new images are associated with the post

    Scenario: AC18 - Edit post with an invalid location
        Given I am editing an existing post and enter location text without selecting a suggested location
        When I click 'Update Post'
        Then I see the edit post error message "This isn't a valid location"
        And the post is not updated
