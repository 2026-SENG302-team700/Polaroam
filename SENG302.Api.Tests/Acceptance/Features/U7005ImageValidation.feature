Feature: U7005 As Benji I want image uploads to be validated So that only appropriately sized and supported images are added to my posts.

    Scenario: AC7 - Image too large
        Given I have selected some files in the file selector And at least one is larger than 10 MB
        When I click 'Select'
        Then I see an image validation error message '{filename} Image too large, maximum file size is 10MB'

    Scenario: AC8 - Invalid image type
        Given I have selected some files in the file selector And at least one is not of the type jpeg, png, svg, webp, or gif
        When I click 'Select'
        Then I see an image validation error message '{filename} Invalid image, supported file types are .jpeg, .png, .svg, webp, .gif'