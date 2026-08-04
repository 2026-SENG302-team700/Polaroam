Feature: U7008_As Benji I want to delete posts So that I can remove posts that I no longer want on Polaroam.
    
    Scenario: AC1 - Delete Posts within given trip
        Given I am viewing a trip and the trip contains posts
        When I delete the tripp
        Then the posts are removed
        And their contents within trip are deleted as well
    
    Scenario: AC2 - Confirm post deletion
        Given I have an existing post to delete
        When I confirm deletion of the post
        Then the post deletion is successful
        And the post is removed from the database
        And the post location is removed from the database
        And the trip containing the post remains