Feature: U70011 - As Benji I want to have my travel images automatically tagged by AI when I upload them to identify key objects and locations in the image so that I can organise and find my memories without having to manually tag every image.

Scenario: AC1 - AI image tagging setting disabled by default
    Given I have just registered for Polaroam
    When I check my settings
    Then I can see that AI image tagging is disabled

Scenario: AC5.1 - Toggle AI image tagging on
    Given I have accepted the AI privacy policy
    When I toggle AI image tagging on
    Then AI image tagging is enabled

Scenario: AC5.2 - Toggle AI image tagging off
    Given I have accepted the AI privacy policy 
    And AI image tagging is enabled
    When I toggle AI image tagging off
    Then AI image tagging is disabled