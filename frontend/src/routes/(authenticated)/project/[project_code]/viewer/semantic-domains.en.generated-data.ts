'use strict';

// This file is essentially a copy of:
// https://github.com/sillsdev/web-languageforge/blob/bug/backup-script-not-adapted-for-mongo-auth/src/angular-app/languageforge/core/semantic-domains/semantic-domains.en.generated-data.js
// Then
// 1) Convert to array
// 2) Replace `(\s*)'key' : (.*),` => `$1'id' : $2,\n$1'code' : $2,`
// 3) Replace `'name' : '(.*)'` => `'name' : { 'en': '$1' }`
export const SEMANTIC_DOMAINS_EN = [
  {
    'guid' : '63403699-07c1-43f3-a47c-069d6e4316e5',
    'id' : '1',
    'code' : '1',
    'abbr' : '1',
    'name' : { 'en': 'Universe, creation' },
    'description' : 'Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as "rain, soil, and things of the sky" or "sky, land, and water" or a descriptive phrase such as "everything you can see" or "everything that exists".',
    'value' : '1 Universe, creation'
  },
  {
    'guid' : '999581c4-1611-4acb-ae1b-5e6c1dfe6f0c',
    'id' : '1.1',
    'code' : '1.1',
    'abbr' : '1.1',
    'name' : { 'en': 'Sky' },
    'description' : 'Use this domain for words related to the sky.',
    'value' : '1.1 Sky'
  },
  {
    'guid' : 'dc1a2c6f-1b32-4631-8823-36dacc8cb7bb',
    'id' : '1.1.1',
    'code' : '1.1.1',
    'abbr' : '1.1.1',
    'name' : { 'en': 'Sun' },
    'description' : 'Use this domain for words related to the sun. The sun does three basic things. It moves, it gives light, and it gives heat. These three actions are involved in the meanings of most of the words in this domain. Since the sun moves below the horizon, many words refer to it setting or rising. Since the sun is above the clouds, many words refer to it moving behind the clouds and the clouds blocking its light. The sun"s light and heat also produce secondary effects. The sun causes plants to grow, and it causes damage to things.',
    'value' : '1.1.1 Sun'
  },
  {
    'guid' : '1bd42665-0610-4442-8d8d-7c666fee3a6d',
    'id' : '1.1.1.1',
    'code' : '1.1.1.1',
    'abbr' : '1.1.1.1',
    'name' : { 'en': 'Moon' },
    'description' : 'Use this domain for words related to the moon. In your culture people may believe things about the moon. For instance in European culture people used to believe that the moon caused people to become crazy. So in English we have words like "moon-struck" and "lunatic." You should include such words in this domain.',
    'value' : '1.1.1.1 Moon'
  },
  {
    'guid' : 'b044e890-ce30-455c-aede-7e9d5569396e',
    'id' : '1.1.1.2',
    'code' : '1.1.1.2',
    'abbr' : '1.1.1.2',
    'name' : { 'en': 'Star' },
    'description' : 'Use this domain for words related to the stars and other heavenly bodies.',
    'value' : '1.1.1.2 Star'
  },
  {
    'guid' : 'a0d073df-d413-4dfd-9ba1-c3c68f126d90',
    'id' : '1.1.1.3',
    'code' : '1.1.1.3',
    'abbr' : '1.1.1.3',
    'name' : { 'en': 'Planet' },
    'description' : 'Use this domain for words related to planets (large objects that circle the sun, looking like bright wandering stars in the sky), comets (objects that circle the sun, looking like a star with a tail), meteors (small objects that come from space and burn up when they hit the earth"s atmosphere, causing a streak of light across the sky), and asteroids (small objects that circle the sun), planetary moons (large objects that circle the planets). Some cultures do not study the stars and will have few or no words in this domain. Others cultures that study the stars will have many words. There are only five planets that people can see in the sky--Mercury, Venus, Mars, Jupiter, and Saturn. The others are only known from the scientific study of astronomy.',
    'value' : '1.1.1.3 Planet'
  },
  {
    'guid' : 'e836b01b-6c1a-4d41-b90a-ea5f349f88d4',
    'id' : '1.1.2',
    'code' : '1.1.2',
    'abbr' : '1.1.2',
    'name' : { 'en': 'Air' },
    'description' : 'Use this domain for words related to the air around us, including the air we breathe and the atmosphere around the earth.',
    'value' : '1.1.2 Air'
  },
  {
    'guid' : '18595df7-1c69-40db-a7c1-74d490115c0c',
    'id' : '1.1.2.1',
    'code' : '1.1.2.1',
    'abbr' : '1.1.2.1',
    'name' : { 'en': 'Blow air' },
    'description' : 'Use this domain for words related to causing air to move.',
    'value' : '1.1.2.1 Blow air'
  },
  {
    'guid' : 'b4aa4bbd-8abf-4503-96e4-05c75efd23d5',
    'id' : '1.1.3',
    'code' : '1.1.3',
    'abbr' : '1.1.3',
    'name' : { 'en': 'Weather' },
    'description' : 'Use this domain for words related to the weather.',
    'value' : '1.1.3 Weather'
  },
  {
    'guid' : '93b8bd61-137a-4ebc-b12f-52fa5d2b3ea4',
    'id' : '1.1.3.1',
    'code' : '1.1.3.1',
    'abbr' : '1.1.3.1',
    'name' : { 'en': 'Wind' },
    'description' : 'Use this domain for words related to the wind. Some words refer to when the wind begins and ends. The wind changes in speed, so some words refer to how fast the wind is moving. Try to rank these on a scale from very slow to very fast. These words may also be distinguished by what the wind does, since a fast wind does more things. These words may also be distinguished by how long the wind blows. Some words refer to the speed of the wind becoming faster or slower. Some words distinguish a steady wind from a wind in which the speed keeps changing. Some words refer to when the speed of the wind becomes faster for a short time. A steady wind moves in a particular direction, so there are words that include the direction of the wind. The direction of the wind may refer to the points of the compass, a neighboring geographical feature or area, or the direction in which the speaker is moving. Some words refer to a wind that moves in a small circle, making a pillar of dust or a funnel-shaped cloud. Some words refer to what the wind does, such as when it moves or damages something. People can feel the wind, so some words refer to how it feels. The wind makes noise, so there are words that refer to the sound of the wind. In some cultures there is a relation between the wind and spirits, so some words may refer to the activity of the spirits in the wind, or that the wind brings disease.',
    'value' : '1.1.3.1 Wind'
  },
  {
    'guid' : '5b12ea7b-790f-4f3e-8d07-893fc267773e',
    'id' : '1.1.3.2',
    'code' : '1.1.3.2',
    'abbr' : '1.1.3.2',
    'name' : { 'en': 'Cloud' },
    'description' : 'Use this domain for words related to the clouds.',
    'value' : '1.1.3.2 Cloud'
  },
  {
    'guid' : 'bfa3be74-0390-4e2e-bdb7-ed41eb67e4f1',
    'id' : '1.1.3.3',
    'code' : '1.1.3.3',
    'abbr' : '1.1.3.3',
    'name' : { 'en': 'Rain' },
    'description' : 'Use this domain for words related to the rain.',
    'value' : '1.1.3.3 Rain'
  },
  {
    'guid' : 'ab8f12fb-57b0-4d61-8ae0-50d7cbc412df',
    'id' : '1.1.3.4',
    'code' : '1.1.3.4',
    'abbr' : '1.1.3.4',
    'name' : { 'en': 'Snow, ice' },
    'description' : 'Use this domain for words related to snow, ice, sleet, and hail.',
    'value' : '1.1.3.4 Snow, ice'
  },
  {
    'guid' : '380b0d15-77a1-49ba-ad83-a508e7ffb83d',
    'id' : '1.1.3.5',
    'code' : '1.1.3.5',
    'abbr' : '1.1.3.5',
    'name' : { 'en': 'Storm' },
    'description' : 'Use this domain for words related to storms.',
    'value' : '1.1.3.5 Storm'
  },
  {
    'guid' : '63c69d11-1101-4870-aeb8-43ee364381b0',
    'id' : '1.1.3.6',
    'code' : '1.1.3.6',
    'abbr' : '1.1.3.6',
    'name' : { 'en': 'Lightning, thunder' },
    'description' : 'Use this domain for words related to lightning and thunder.',
    'value' : '1.1.3.6 Lightning, thunder'
  },
  {
    'guid' : '349937e3-a2fd-41f8-b7c4-bd6fa106add4',
    'id' : '1.1.3.7',
    'code' : '1.1.3.7',
    'abbr' : '1.1.3.7',
    'name' : { 'en': 'Flood' },
    'description' : 'Use this domain for words related to floods.',
    'value' : '1.1.3.7 Flood'
  },
  {
    'guid' : 'e6b21531-b7d0-4e37-b01b-3ca49a285168',
    'id' : '1.1.3.8',
    'code' : '1.1.3.8',
    'abbr' : '1.1.3.8',
    'name' : { 'en': 'Drought' },
    'description' : 'Use this domain for words related to drought.',
    'value' : '1.1.3.8 Drought'
  },
  {
    'guid' : 'b47d2604-8b23-41e9-9158-01526dd83894',
    'id' : '1.2',
    'code' : '1.2',
    'abbr' : '1.2',
    'name' : { 'en': 'World' },
    'description' : 'Use this domain for words referring to the planet we live on.',
    'value' : '1.2 World'
  },
  {
    'guid' : 'cce98603-ff8f-4213-945a-bd6746716139',
    'id' : '1.2.1',
    'code' : '1.2.1',
    'abbr' : '1.2.1',
    'name' : { 'en': 'Land' },
    'description' : 'Use this domain for words referring to the ground we stand on, the earth versus the sky.',
    'value' : '1.2.1 Land'
  },
  {
    'guid' : '0ac5e5f9-e7fe-4d37-a631-eab1ceb1f8ae',
    'id' : '1.2.1.1',
    'code' : '1.2.1.1',
    'abbr' : '1.2.1.1',
    'name' : { 'en': 'Mountain' },
    'description' : 'Use this domain for words related to mountains.',
    'value' : '1.2.1.1 Mountain'
  },
  {
    'guid' : 'd50f3921-fcea-4ac9-b64a-25bf47dc3292',
    'id' : '1.2.1.2',
    'code' : '1.2.1.2',
    'abbr' : '1.2.1.2',
    'name' : { 'en': 'Volcano' },
    'description' : 'Use this domain for words related to volcanoes.',
    'value' : '1.2.1.2 Volcano'
  },
  {
    'guid' : '4fb79b12-3bd1-46ed-8698-7d27052a5dc7',
    'id' : '1.2.1.3',
    'code' : '1.2.1.3',
    'abbr' : '1.2.1.3',
    'name' : { 'en': 'Plain, plateau' },
    'description' : 'Use this domain for words referring to land that is flat.',
    'value' : '1.2.1.3 Plain, plateau'
  },
  {
    'guid' : 'cd403434-a5a1-4700-8ad3-b7c9aabd99d9',
    'id' : '1.2.1.4',
    'code' : '1.2.1.4',
    'abbr' : '1.2.1.4',
    'name' : { 'en': 'Valley' },
    'description' : 'Use this domain for words related to valleys.',
    'value' : '1.2.1.4 Valley'
  },
  {
    'guid' : 'b3be00a9-41a4-42ae-ba51-320b5000a563',
    'id' : '1.2.1.5',
    'code' : '1.2.1.5',
    'abbr' : '1.2.1.5',
    'name' : { 'en': 'Underground' },
    'description' : 'Use this domain for words referring to the area under the ground, and to holes in the ground.',
    'value' : '1.2.1.5 Underground'
  },
  {
    'guid' : '7988974c-99fd-40dd-9b5e-2d81ec603ddc',
    'id' : '1.2.1.6',
    'code' : '1.2.1.6',
    'abbr' : '1.2.1.6',
    'name' : { 'en': 'Forest, grassland, desert' },
    'description' : 'Use this domain for words referring to an area of land that has particular types of plants growing in it.',
    'value' : '1.2.1.6 Forest, grassland, desert'
  },
  {
    'guid' : 'b3745f13-3632-4f13-b0cc-a74c51f8f2a1',
    'id' : '1.2.1.7',
    'code' : '1.2.1.7',
    'abbr' : '1.2.1.7',
    'name' : { 'en': 'Earthquake' },
    'description' : 'Use this domain for words related to earthquakes. In some languages earthquakes are thought of as moving somewhere. In Amele (PNG) they say "mim nen" which means "the earthquake came down (from above)". In Northern Embera an earthquake is a "house-shaking". They say "a house-shaking went."',
    'value' : '1.2.1.7 Earthquake'
  },
  {
    'guid' : 'f899802d-bd32-427f-a101-c84219f7e14e',
    'id' : '1.2.2',
    'code' : '1.2.2',
    'abbr' : '1.2.2',
    'name' : { 'en': 'Substance, matter' },
    'description' : 'Use this domain for general words referring to matter--what something is made out of, or a type of solid, liquid, or gas.',
    'value' : '1.2.2 Substance, matter'
  },
  {
    'guid' : '180a2220-942c-4e17-96ee-cd4f63a4c715',
    'id' : '1.2.2.1',
    'code' : '1.2.2.1',
    'abbr' : '1.2.2.1',
    'name' : { 'en': 'Soil, dirt' },
    'description' : 'Use this domain for words referring to soil and dirt.',
    'value' : '1.2.2.1 Soil, dirt'
  },
  {
    'guid' : '0f07adb7-4387-4723-9800-8362e825ad45',
    'id' : '1.2.2.2',
    'code' : '1.2.2.2',
    'abbr' : '1.2.2.2',
    'name' : { 'en': 'Rock' },
    'description' : 'Use this domain for words referring to rock.',
    'value' : '1.2.2.2 Rock'
  },
  {
    'guid' : '3df7d174-83d1-4e17-890e-1272e171ca41',
    'id' : '1.2.2.3',
    'code' : '1.2.2.3',
    'abbr' : '1.2.2.3',
    'name' : { 'en': 'Metal' },
    'description' : 'Use this domain for words referring to metal.',
    'value' : '1.2.2.3 Metal'
  },
  {
    'guid' : '56c9c38c-728a-42fe-b93c-6ca67fdf2a9a',
    'id' : '1.2.2.4',
    'code' : '1.2.2.4',
    'abbr' : '1.2.2.4',
    'name' : { 'en': 'Mineral' },
    'description' : 'Use this domain for naturally occurring elements, compounds, and minerals--things you can find in the ground.',
    'value' : '1.2.2.4 Mineral'
  },
  {
    'guid' : '21bcc306-13cb-4162-98b3-2ba319ba14ea',
    'id' : '1.2.2.5',
    'code' : '1.2.2.5',
    'abbr' : '1.2.2.5',
    'name' : { 'en': 'Jewel' },
    'description' : 'Use this domain for words referring to jewels and precious stones.',
    'value' : '1.2.2.5 Jewel'
  },
  {
    'guid' : '756728a8-9eb8-4329-aee2-d6a3d64585f2',
    'id' : '1.2.3',
    'code' : '1.2.3',
    'abbr' : '1.2.3',
    'name' : { 'en': 'Solid, liquid, gas' },
    'description' : 'Use this domain for words describing the different states of matter (solid, liquid, and gas), and words for changing from one to another.',
    'value' : '1.2.3 Solid, liquid, gas'
  },
  {
    'guid' : 'f56a2511-10cc-4829-940d-49051429bfba',
    'id' : '1.2.3.1',
    'code' : '1.2.3.1',
    'abbr' : '1.2.3.1',
    'name' : { 'en': 'Liquid' },
    'description' : 'Use this domain for words referring to liquids.',
    'value' : '1.2.3.1 Liquid'
  },
  {
    'guid' : '962cb994-0183-4ac5-94b2-82a33f1d64e4',
    'id' : '1.2.3.2',
    'code' : '1.2.3.2',
    'abbr' : '1.2.3.2',
    'name' : { 'en': 'Oil' },
    'description' : 'Use this domain for words referring to oil.',
    'value' : '1.2.3.2 Oil'
  },
  {
    'guid' : '34c3edad-a158-44e7-989b-5b74401e6945',
    'id' : '1.2.3.3',
    'code' : '1.2.3.3',
    'abbr' : '1.2.3.3',
    'name' : { 'en': 'Gas' },
    'description' : 'Use this domain for words referring to gas.',
    'value' : '1.2.3.3 Gas'
  },
  {
    'guid' : '60364974-a005-4567-82e9-7aaeff894ab0',
    'id' : '1.3',
    'code' : '1.3',
    'abbr' : '1.3',
    'name' : { 'en': 'Water' },
    'description' : 'Use this domain for general words referring to water.',
    'value' : '1.3 Water'
  },
  {
    'guid' : '79ebb5ce-f0fd-4fb5-9f22-1fa4965a555b',
    'id' : '1.3.1',
    'code' : '1.3.1',
    'abbr' : '1.3.1',
    'name' : { 'en': 'Bodies of water' },
    'description' : 'Use this domain for general words referring to bodies of water.',
    'value' : '1.3.1 Bodies of water'
  },
  {
    'guid' : '14e9c20c-6eb5-49a4-a03f-3be26a934500',
    'id' : '1.3.1.1',
    'code' : '1.3.1.1',
    'abbr' : '1.3.1.1',
    'name' : { 'en': 'Ocean, lake' },
    'description' : 'Use this domain for words referring to bodies of standing water.',
    'value' : '1.3.1.1 Ocean, lake'
  },
  {
    'guid' : '31777669-e37b-4b77-9cce-0d8c33f6ebb9',
    'id' : '1.3.1.2',
    'code' : '1.3.1.2',
    'abbr' : '1.3.1.2',
    'name' : { 'en': 'Swamp' },
    'description' : 'Use this domain for words referring to bodies of standing water with plants growing in them.',
    'value' : '1.3.1.2 Swamp'
  },
  {
    'guid' : '4153416a-784d-4f7c-a664-2640f7979a14',
    'id' : '1.3.1.3',
    'code' : '1.3.1.3',
    'abbr' : '1.3.1.3',
    'name' : { 'en': 'River' },
    'description' : 'Use this domain for words referring to bodies of flowing water.',
    'value' : '1.3.1.3 River'
  },
  {
    'guid' : 'bf6e1719-11ee-4ace-9c84-72019c01aabc',
    'id' : '1.3.1.4',
    'code' : '1.3.1.4',
    'abbr' : '1.3.1.4',
    'name' : { 'en': 'Spring, well' },
    'description' : 'Use this domain for words referring to a place where water comes out of the ground.',
    'value' : '1.3.1.4 Spring, well'
  },
  {
    'guid' : '928741b5-bff6-4dd1-be37-ec6e7a4eb6ca',
    'id' : '1.3.1.5',
    'code' : '1.3.1.5',
    'abbr' : '1.3.1.5',
    'name' : { 'en': 'Island, shore' },
    'description' : 'Use this domain for words referring to land in contrast with the sea or river.',
    'value' : '1.3.1.5 Island, shore'
  },
  {
    'guid' : '50ab3705-a81e-4fcc-b3ae-95c075966f69',
    'id' : '1.3.2',
    'code' : '1.3.2',
    'abbr' : '1.3.2',
    'name' : { 'en': 'Movement of water' },
    'description' : 'Use this domain for words referring to the way in which water and other liquids move.',
    'value' : '1.3.2 Movement of water'
  },
  {
    'guid' : '60595d09-4a15-4499-b6e1-d36a704bcbe9',
    'id' : '1.3.2.1',
    'code' : '1.3.2.1',
    'abbr' : '1.3.2.1',
    'name' : { 'en': 'Flow' },
    'description' : 'Use this domain for words referring to the way water moves over a surface, such as in a river or along the ground.',
    'value' : '1.3.2.1 Flow'
  },
  {
    'guid' : '647603c2-6f32-48f3-91fa-1f7f0e44b539',
    'id' : '1.3.2.2',
    'code' : '1.3.2.2',
    'abbr' : '1.3.2.2',
    'name' : { 'en': 'Pour' },
    'description' : 'Use this domain for words referring to water coming out of something (such as a container), or causing water to come out of something.',
    'value' : '1.3.2.2 Pour'
  },
  {
    'guid' : 'a9fbc056-3134-41af-baf4-9f63fa5bd5ae',
    'id' : '1.3.2.3',
    'code' : '1.3.2.3',
    'abbr' : '1.3.2.3',
    'name' : { 'en': 'Drip' },
    'description' : 'Use this domain for words referring to drops of water and what they do.',
    'value' : '1.3.2.3 Drip'
  },
  {
    'guid' : '741c417a-11e9-460c-9ab3-51b8220df016',
    'id' : '1.3.2.4',
    'code' : '1.3.2.4',
    'abbr' : '1.3.2.4',
    'name' : { 'en': 'Wave' },
    'description' : 'Use this domain for words related to waves and what they do.',
    'value' : '1.3.2.4 Wave'
  },
  {
    'guid' : 'b09205d4-fbb4-4bcd-94ef-f8d83e298462',
    'id' : '1.3.2.5',
    'code' : '1.3.2.5',
    'abbr' : '1.3.2.5',
    'name' : { 'en': 'Calm, rough' },
    'description' : 'Use this domain for words describing the surface of water.',
    'value' : '1.3.2.5 Calm, rough'
  },
  {
    'guid' : 'c1a63ba2-1db6-410d-a4ed-5f64d1798bc1',
    'id' : '1.3.2.6',
    'code' : '1.3.2.6',
    'abbr' : '1.3.2.6',
    'name' : { 'en': 'Tide' },
    'description' : 'Use this domain for words related to the tide.',
    'value' : '1.3.2.6 Tide'
  },
  {
    'guid' : 'f38f8344-838f-44ba-b103-22289c2d2793',
    'id' : '1.3.3',
    'code' : '1.3.3',
    'abbr' : '1.3.3',
    'name' : { 'en': 'Wet' },
    'description' : 'Use this domain for words referring to when something has water on it or water has soaked into it.',
    'value' : '1.3.3 Wet'
  },
  {
    'guid' : '0b0801a3-8a0c-40ea-bf41-07df80bd0d5f',
    'id' : '1.3.3.1',
    'code' : '1.3.3.1',
    'abbr' : '1.3.3.1',
    'name' : { 'en': 'Dry' },
    'description' : 'Use this domain for words describing something that is dry.',
    'value' : '1.3.3.1 Dry'
  },
  {
    'guid' : 'bf25931d-4760-4c66-abe8-c05a6dc5adbe',
    'id' : '1.3.4',
    'code' : '1.3.4',
    'abbr' : '1.3.4',
    'name' : { 'en': 'Be in water' },
    'description' : 'Use this domain for words referring to being in water or putting something in water.',
    'value' : '1.3.4 Be in water'
  },
  {
    'guid' : 'e7e5dbf2-6d5b-4869-b357-8a7860c29002',
    'id' : '1.3.5',
    'code' : '1.3.5',
    'abbr' : '1.3.5',
    'name' : { 'en': 'Solutions of water' },
    'description' : 'Use this domain for words referring to a mixture of water and a substance (such as salt or sugar) that dissolves in water.',
    'value' : '1.3.5 Solutions of water'
  },
  {
    'guid' : '4d19f09f-035b-477e-862c-a4157acdfe81',
    'id' : '1.3.6',
    'code' : '1.3.6',
    'abbr' : '1.3.6',
    'name' : { 'en': 'Water quality' },
    'description' : 'Use this domain for words describing the quality or condition of water.',
    'value' : '1.3.6 Water quality'
  },
  {
    'guid' : '8d47c9ec-80c4-4309-9848-c453dcd71182',
    'id' : '1.4',
    'code' : '1.4',
    'abbr' : '1.4',
    'name' : { 'en': 'Living things' },
    'description' : 'Use this domain for general words that relate to all living things.',
    'value' : '1.4 Living things'
  },
  {
    'guid' : '06a89652-70e0-40ac-b929-ed42f011c9fc',
    'id' : '1.4.1',
    'code' : '1.4.1',
    'abbr' : '1.4.1',
    'name' : { 'en': 'Dead things' },
    'description' : 'Use this domain for words referring to dead things--things that were alive before, but aren"t now.',
    'value' : '1.4.1 Dead things'
  },
  {
    'guid' : '1c512719-6ecb-48cb-980e-4ff20e8b5f9b',
    'id' : '1.4.2',
    'code' : '1.4.2',
    'abbr' : '1.4.2',
    'name' : { 'en': 'Spirits of things' },
    'description' : 'Use this domain for words referring to the spirits of things.',
    'value' : '1.4.2 Spirits of things'
  },
  {
    'guid' : '025da6f4-b1b6-423a-8c0f-b324f531a6f1',
    'id' : '1.5',
    'code' : '1.5',
    'abbr' : '1.5',
    'name' : { 'en': 'Plant' },
    'description' : 'Use this domain for general words for all plants. Use a book of pictures to identify plant names and the scientific name. Languages divide plants into various domains that are not always comparable from language to language. Criterial features may be characteristics (trees and bushes are distinguished by size and number of trunks) and use (grass and weeds are distinguished by their desirability). A common distinction is between trees and non-trees, with trees described as being big, woody, and having a life expectancy of several years, while non-trees are small, non-woody, and have a life expectancy of typically not more than one year (Heine, Bernd and Karsten Legere. 1995. Swahili plants. Rudiger Koppe Verlag: Koln.). Agricultural societies will divide plants into wild and cultivated. However most plants for which there are names have some use. Therefore it does not seem helpful to divide plant names into domains for useful and non-useful plants. Since only parts of plants are eaten, edible parts of plants are listed under the domain "Food". Some languages may have more domains than are used in this list, others may have fewer. The classification system used here does not agree entirely with the system used by botanists. For instance botanists do not classify all the tree species together. The palm trees belong to the class Monocotyledoneae and are classified with lilies, bananas, and orchids. Apple and cherry trees belong to the class Dicotyledoneae and are classified in the rose family along with roses and blackberries. The acacia tree also belongs to the class Dicotyledoneae and is classified in the pulse family along with lupines and beans. However most folk taxonomies bring all the trees together. The scientific classification system for plants and animals is taken from: Carruth, Gorton, ed. 1989. The Volume Library, Vols. 1 and 2. The Southwestern Company: Nashville.',
    'value' : '1.5 Plant'
  },
  {
    'guid' : '0ee5b933-f1ab-485f-894a-51fe239cb726',
    'id' : '1.5.1',
    'code' : '1.5.1',
    'abbr' : '1.5.1',
    'name' : { 'en': 'Tree' },
    'description' : 'Use this domain for trees--flowering plants with roots, stems, and leaves, which are large and have a wooden trunk (Phylum Spermatophyta, Subdivision Angiospermae). Also include the evergreen trees (Phylum Spermatophyta, Subdivision Gymnospermae). Evergreen trees do not have flowers, but have cone like fruits (pinecones) that contain seeds. Their leaves are shaped like needles and are retained for over a year.',
    'value' : '1.5.1 Tree'
  },
  {
    'guid' : '531af868-b5fb-41c2-ba50-764458f9102f',
    'id' : '1.5.2',
    'code' : '1.5.2',
    'abbr' : '1.5.2',
    'name' : { 'en': 'Bush, shrub' },
    'description' : 'Use this domain for bushes and shrubs--plants that are smaller than trees and have several wooden trunks (Phylum Spermatophyta, Subdivision Angiospermae).',
    'value' : '1.5.2 Bush, shrub'
  },
  {
    'guid' : 'c345f278-91ff-463d-b9a6-8abac8a267eb',
    'id' : '1.5.3',
    'code' : '1.5.3',
    'abbr' : '1.5.3',
    'name' : { 'en': 'Grass, herb, vine' },
    'description' : 'Use this domain for small plants that have roots, stems, flowers, and seeds, but do not have a wooden trunk (Phylum Spermatophyta, Subdivision Angiospermae). Also include the seedless plants, such as ferns (phylum Pteridophyta, class Felicineae), horsetails (phylum Pteridophyta, class Equisetineae), and club mosses (phylum Pteridophyta, class Lycopodineae). Plants of the Pteridophyta phylum have no flowers or seeds. Ferns have large, divided, feather-like leaves, or fronds. Club mosses are small (rarely over one meter) evergreen plants with simple leaves resembling pine or hemlock needles. Sometimes they grow upright, but often trail on the ground, where they propagate by means of runners. Horsetails send up tall, vertical, jointed stalks with branches covered with scale like leaves.',
    'value' : '1.5.3 Grass, herb, vine'
  },
  {
    'guid' : 'd06dae77-134a-403f-ba88-52ecd66c0522',
    'id' : '1.5.4',
    'code' : '1.5.4',
    'abbr' : '1.5.4',
    'name' : { 'en': 'Moss, fungus, algae' },
    'description' : 'Use this domain for mosses (phylum Bryophyta, class Musci), liverworts (phylum Bryophyta, class Hepaticae), fungi (phylum Thallophyta, subdivision Fungi), algae (phylum Thallophyta, subdivision Algae), and lichens. These plants do not have true roots, stems, or leaves. The mosses are small, green, flowerless plants that grow in moist environments and look like velvety or feathery growths carpeting the ground, tree trunks, and rocks. The liverworts are similar to the mosses with a flat and branching growth pattern. The algae possess green chlorophyll. They vary from one-celled organisms, which sometimes live in colonies such as pond scum, to complex organisms such as seaweed. The fungi do not possess chlorophyll and feed off of other organic material. Lichens consist of a fungus and an algae growing together. They commonly grow on trunks of trees and rocks. Some are flat and leafy, and some are moss like.',
    'value' : '1.5.4 Moss, fungus, algae'
  },
  {
    'guid' : 'd117aa22-3f18-47c4-9683-51ecf1dc7134',
    'id' : '1.5.5',
    'code' : '1.5.5',
    'abbr' : '1.5.5',
    'name' : { 'en': 'Parts of a plant' },
    'description' : 'Use this domain for words that refer to parts of a plant. Start with general words that all plants have. Then think through each major type of plant. Finish by thinking of specific plants that are well known (usually cultivated crops) and that have words for specific parts (e.g. tassel on a corn/maize cob).',
    'value' : '1.5.5 Parts of a plant'
  },
  {
    'guid' : 'd2ca3194-e393-480e-ae1d-dd67bed55227',
    'id' : '1.5.6',
    'code' : '1.5.6',
    'abbr' : '1.5.6',
    'name' : { 'en': 'Growth of plants' },
    'description' : 'Use this domain for words related to the growth of plants.',
    'value' : '1.5.6 Growth of plants'
  },
  {
    'guid' : 'f7b7eb3c-b784-4ba5-8dac-a68fd27ce0ea',
    'id' : '1.5.7',
    'code' : '1.5.7',
    'abbr' : '1.5.7',
    'name' : { 'en': 'Plant diseases' },
    'description' : 'Use this domain for words related to plant diseases.',
    'value' : '1.5.7 Plant diseases'
  },
  {
    'guid' : '944cf5af-469e-4b03-878f-a05d34b0d9f6',
    'id' : '1.6',
    'code' : '1.6',
    'abbr' : '1.6',
    'name' : { 'en': 'Animal' },
    'description' : 'Use this domain for general words referring to animals.',
    'value' : '1.6 Animal'
  },
  {
    'guid' : '73499b8b-76fc-4121-8bfa-1bdebe537259',
    'id' : '1.6.1',
    'code' : '1.6.1',
    'abbr' : '1.6.1',
    'name' : { 'en': 'Types of animals' },
    'description' : 'Use this domain for words describing types of animals. Use a book of pictures to identify each species and its scientific name. This section is organized according to the scientific, biological classification of animals. It may not correspond to the local classification of animals (folk taxonomy), which are often based on how people relate to animals (tame/wild, edible/work). Use this domain for words referring to large classes of animals that do not correspond to the scientific classification. For instance this would be the place for a word like "flying animal", which includes birds, bats, and flying insects.',
    'value' : '1.6.1 Types of animals'
  },
  {
    'guid' : '5e9f361d-17dc-4ada-a312-8da269d22a64',
    'id' : '1.6.1.1',
    'code' : '1.6.1.1',
    'abbr' : '1.6.1.1',
    'name' : { 'en': 'Mammal' },
    'description' : 'Use this domain for general words referring to mammals (phylum Chordata, class Mammalia).',
    'value' : '1.6.1.1 Mammal'
  },
  {
    'guid' : '40248a12-1809-4561-b786-e4e274c14d82',
    'id' : '1.6.1.1.1',
    'code' : '1.6.1.1.1',
    'abbr' : '1.6.1.1.1',
    'name' : { 'en': 'Primate' },
    'description' : 'Use this domain for primates (phylum Chordata, class Mammalia, order Primates).',
    'value' : '1.6.1.1.1 Primate'
  },
  {
    'guid' : '56ef3f06-7fb9-462e-a7d0-517f3ce1623f',
    'id' : '1.6.1.1.2',
    'code' : '1.6.1.1.2',
    'abbr' : '1.6.1.1.2',
    'name' : { 'en': 'Carnivore' },
    'description' : 'Use this domain for carnivores--meat-eating animals (phylum Chordata, class Mammalia, order Carnivora).',
    'value' : '1.6.1.1.2 Carnivore'
  },
  {
    'guid' : 'bbd3c3f1-7387-4ec6-a75d-66c1355a94ef',
    'id' : '1.6.1.1.3',
    'code' : '1.6.1.1.3',
    'abbr' : '1.6.1.1.3',
    'name' : { 'en': 'Hoofed animals' },
    'description' : 'Use this domain for even-toed hoofed animals (phylum Chordata, class Mammalia, order Artiodactyla), odd-toed hoofed animals (phylum Chordata, class Mammalia, order Perissodactyla), and elephants (phylum Chordata, class Mammalia, order Proboscidea).',
    'value' : '1.6.1.1.3 Hoofed animals'
  },
  {
    'guid' : 'a8b9e892-df3e-44c4-8c68-7a0f7f4468bb',
    'id' : '1.6.1.1.4',
    'code' : '1.6.1.1.4',
    'abbr' : '1.6.1.1.4',
    'name' : { 'en': 'Rodent' },
    'description' : 'Use this domain for rodents--gnawing animals (phylum Chordata, class Mammalia, order Rodentia), insect eating animals (phylum Chordata, class Mammalia, order Insectivora), rabbits (phylum Chordata, class Mammalia, order Lagomorpha), and hyraxes (phylum Chordata, class Mammalia, order Hyracoidea).',
    'value' : '1.6.1.1.4 Rodent'
  },
  {
    'guid' : '31171aa9-e243-4b46-abd8-f3e52843cdfc',
    'id' : '1.6.1.1.5',
    'code' : '1.6.1.1.5',
    'abbr' : '1.6.1.1.5',
    'name' : { 'en': 'Marsupial' },
    'description' : 'Use this domain for marsupials (phylum Chordata, class Mammalia, order Marsupialia). Marsupials carry their young in a pouch.',
    'value' : '1.6.1.1.5 Marsupial'
  },
  {
    'guid' : '445f3084-f250-40fa-87ba-ebd233f9018f',
    'id' : '1.6.1.1.6',
    'code' : '1.6.1.1.6',
    'abbr' : '1.6.1.1.6',
    'name' : { 'en': 'Anteater, aardvark' },
    'description' : 'Use this domain for mammals with few or no teeth--anteaters (phylum Chordata, class Mammalia, order Edentata), pangolins (phylum Chordata, class Mammalia, order Pholidota), aardvarks (phylum Chordata, class Mammalia, order Tubulidentata), and platypus--mammals that lay eggs (phylum Chordata, class Mammalia, order Monotremata).',
    'value' : '1.6.1.1.6 Anteater, aardvark'
  },
  {
    'guid' : 'e22d860a-d207-4649-8ab5-4592b838febb',
    'id' : '1.6.1.1.7',
    'code' : '1.6.1.1.7',
    'abbr' : '1.6.1.1.7',
    'name' : { 'en': 'Sea mammal' },
    'description' : 'Use this domain for mammals that live in the sea--whales and dolphins (phylum Chordata, class Mammalia, order Cetacea), seals (phylum Chordata, class Mammalia, order Pinnipedia), and sea cows (phylum Chordata, class Mammalia, order Sirenia).',
    'value' : '1.6.1.1.7 Sea mammal'
  },
  {
    'guid' : '2c322d8b-d762-43ce-b905-aab41f9c7bbb',
    'id' : '1.6.1.1.8',
    'code' : '1.6.1.1.8',
    'abbr' : '1.6.1.1.8',
    'name' : { 'en': 'Bat' },
    'description' : 'Use this domain for bats--flying mammals (phylum Chordata, class Mammalia, order Chiroptera).',
    'value' : '1.6.1.1.8 Bat'
  },
  {
    'guid' : '83b483b8-f036-44be-8510-ea337d010a1c',
    'id' : '1.6.1.2',
    'code' : '1.6.1.2',
    'abbr' : '1.6.1.2',
    'name' : { 'en': 'Bird' },
    'description' : 'Use this domain for birds (phylum Chordata, class Aves).',
    'value' : '1.6.1.2 Bird'
  },
  {
    'guid' : 'ee446395-781b-4651-afef-cad78b71f843',
    'id' : '1.6.1.3',
    'code' : '1.6.1.3',
    'abbr' : '1.6.1.3',
    'name' : { 'en': 'Reptile' },
    'description' : 'Use this domain for general words referring to reptiles (phylum Chordata, class Reptilia).',
    'value' : '1.6.1.3 Reptile'
  },
  {
    'guid' : '7d472317-b5e8-4cae-a03f-913ecdaf4c29',
    'id' : '1.6.1.3.1',
    'code' : '1.6.1.3.1',
    'abbr' : '1.6.1.3.1',
    'name' : { 'en': 'Snake' },
    'description' : 'Use this domain for words related to snakes.',
    'value' : '1.6.1.3.1 Snake'
  },
  {
    'guid' : '36b3cfb6-0fea-4628-aa8d-f9b7af48f436',
    'id' : '1.6.1.3.2',
    'code' : '1.6.1.3.2',
    'abbr' : '1.6.1.3.2',
    'name' : { 'en': 'Lizard' },
    'description' : 'Use this domain for words related to lizards.',
    'value' : '1.6.1.3.2 Lizard'
  },
  {
    'guid' : 'faf0ae24-6584-4766-a93b-389c1cb06d8d',
    'id' : '1.6.1.3.3',
    'code' : '1.6.1.3.3',
    'abbr' : '1.6.1.3.3',
    'name' : { 'en': 'Turtle' },
    'description' : 'Use this domain for words related to turtles.',
    'value' : '1.6.1.3.3 Turtle'
  },
  {
    'guid' : 'c21c28e8-9731-4ee0-acbb-32501bf8abd1',
    'id' : '1.6.1.3.4',
    'code' : '1.6.1.3.4',
    'abbr' : '1.6.1.3.4',
    'name' : { 'en': 'Crocodile' },
    'description' : 'Use this domain for words referring to crocodiles.',
    'value' : '1.6.1.3.4 Crocodile'
  },
  {
    'guid' : '8d8a7656-8f8e-467e-b72e-535db6a17c6a',
    'id' : '1.6.1.4',
    'code' : '1.6.1.4',
    'abbr' : '1.6.1.4',
    'name' : { 'en': 'Amphibian' },
    'description' : 'Use this domain for amphibians (phylum Chordata, class Amphibia).',
    'value' : '1.6.1.4 Amphibian'
  },
  {
    'guid' : '85188748-1919-4210-a9e9-91171d9d6454',
    'id' : '1.6.1.5',
    'code' : '1.6.1.5',
    'abbr' : '1.6.1.5',
    'name' : { 'en': 'Fish' },
    'description' : 'Use this domain for fish (phylum Chordata, class Osteichthyes).',
    'value' : '1.6.1.5 Fish'
  },
  {
    'guid' : '3014de03-88e5-4330-9682-51963a41ca50',
    'id' : '1.6.1.6',
    'code' : '1.6.1.6',
    'abbr' : '1.6.1.6',
    'name' : { 'en': 'Shark, ray' },
    'description' : 'Use this domain for sharks and rays--animals with cartilage instead of bones (phylum Chordata, class Chondrichthyes), and eels (phylum Chordata, class Cyclostomata).',
    'value' : '1.6.1.6 Shark, ray'
  },
  {
    'guid' : 'ecc39bc2-6336-48ca-be46-cf5e49a3c267',
    'id' : '1.6.1.7',
    'code' : '1.6.1.7',
    'abbr' : '1.6.1.7',
    'name' : { 'en': 'Insect' },
    'description' : 'Use this domain for the names of insect species (phylum Arthropoda, class Insecta). Note that insects have six legs and spiders have eight legs. However some languages may not distinguish insects from spiders and may use other characteristics to sub-divide the Arthropods.',
    'value' : '1.6.1.7 Insect'
  },
  {
    'guid' : 'cfb159f7-82f6-4789-b9b4-8f611820f350',
    'id' : '1.6.1.8',
    'code' : '1.6.1.8',
    'abbr' : '1.6.1.8',
    'name' : { 'en': 'Spider' },
    'description' : 'Use this domain for spiders (phylum Arthropoda, class Arachnida). Note that insects have six legs and spiders have eight legs. However some languages may not distinguish insects from spiders and may use other characteristics to sub-divide the Arthropods.',
    'value' : '1.6.1.8 Spider'
  },
  {
    'guid' : '38473463-4b92-4681-8fd0-0aca0342e88a',
    'id' : '1.6.1.9',
    'code' : '1.6.1.9',
    'abbr' : '1.6.1.9',
    'name' : { 'en': 'Small animals' },
    'description' : 'Use this domain for the names of worms, animals with shells, and other animals that do not fit into any of the other categories.',
    'value' : '1.6.1.9 Small animals'
  },
  {
    'guid' : 'ffd0547e-e537-4614-ac3f-6d8cd3351f33',
    'id' : '1.6.2',
    'code' : '1.6.2',
    'abbr' : '1.6.2',
    'name' : { 'en': 'Parts of an animal' },
    'description' : 'Use this domain for the parts of animals, especially those of mammals.',
    'value' : '1.6.2 Parts of an animal'
  },
  {
    'guid' : 'e6221c7a-4608-4114-ba9f-532a3b943113',
    'id' : '1.6.2.1',
    'code' : '1.6.2.1',
    'abbr' : '1.6.2.1',
    'name' : { 'en': 'Parts of a bird' },
    'description' : 'Use this domain for the parts of a bird, but not general parts that belong to all animals.',
    'value' : '1.6.2.1 Parts of a bird'
  },
  {
    'guid' : '203f46d2-f0d0-4dda-8d1a-ddc15065b005',
    'id' : '1.6.2.2',
    'code' : '1.6.2.2',
    'abbr' : '1.6.2.2',
    'name' : { 'en': 'Parts of a reptile' },
    'description' : 'Use this domain for the parts of a reptile.',
    'value' : '1.6.2.2 Parts of a reptile'
  },
  {
    'guid' : '73f3bd83-c9a5-4613-aeb1-0c5076d4850e',
    'id' : '1.6.2.3',
    'code' : '1.6.2.3',
    'abbr' : '1.6.2.3',
    'name' : { 'en': 'Parts of a fish' },
    'description' : 'Use this domain for the parts of a fish.',
    'value' : '1.6.2.3 Parts of a fish'
  },
  {
    'guid' : '0a37e7d5-b10e-4f1d-baf0-e71668425b3e',
    'id' : '1.6.2.4',
    'code' : '1.6.2.4',
    'abbr' : '1.6.2.4',
    'name' : { 'en': 'Parts of an insect' },
    'description' : 'Use this domain for the parts of an insect.',
    'value' : '1.6.2.4 Parts of an insect'
  },
  {
    'guid' : '5d72af95-facd-4be2-80e9-37528f0f34b5',
    'id' : '1.6.2.5',
    'code' : '1.6.2.5',
    'abbr' : '1.6.2.5',
    'name' : { 'en': 'Parts of small animals' },
    'description' : 'Use this domain for the parts of small animals.',
    'value' : '1.6.2.5 Parts of small animals'
  },
  {
    'guid' : 'ac187298-85e8-43ed-ba85-cc06a62c08ba',
    'id' : '1.6.3',
    'code' : '1.6.3',
    'abbr' : '1.6.3',
    'name' : { 'en': 'Animal life cycle' },
    'description' : 'Use this domain for words related to the life cycle of an animal.',
    'value' : '1.6.3 Animal life cycle'
  },
  {
    'guid' : 'b6a40216-fe93-4b0f-b85b-5622327031d0',
    'id' : '1.6.3.1',
    'code' : '1.6.3.1',
    'abbr' : '1.6.3.1',
    'name' : { 'en': 'Egg' },
    'description' : 'Use this domain for words related to eggs.',
    'value' : '1.6.3.1 Egg'
  },
  {
    'guid' : 'e76227e8-4a04-4fbd-a16e-5baa3d9e97a9',
    'id' : '1.6.4',
    'code' : '1.6.4',
    'abbr' : '1.6.4',
    'name' : { 'en': 'Animal actions' },
    'description' : 'Use this domain for actions of animals.',
    'value' : '1.6.4 Animal actions'
  },
  {
    'guid' : '284433df-7b37-4e63-a614-78520c483213',
    'id' : '1.6.4.1',
    'code' : '1.6.4.1',
    'abbr' : '1.6.4.1',
    'name' : { 'en': 'Animal movement' },
    'description' : 'Use this domain for ways in which animals move. Only include words specific for the movement of animals. For the movement of people use the domains under Movement. It is necessary to think through how each type of animal moves, especially the important ones.',
    'value' : '1.6.4.1 Animal movement'
  },
  {
    'guid' : '8f68d85f-70f8-4662-9b4b-1dd2900a002a',
    'id' : '1.6.4.2',
    'code' : '1.6.4.2',
    'abbr' : '1.6.4.2',
    'name' : { 'en': 'Animal eating' },
    'description' : 'Use this domain for words referring to animals eating. Because animals often eat in very different ways from people, many languages will have words that are specific to the way an animal eats.',
    'value' : '1.6.4.2 Animal eating'
  },
  {
    'guid' : 'cb99a086-8c6d-4f90-81db-6afa69ae5455',
    'id' : '1.6.4.3',
    'code' : '1.6.4.3',
    'abbr' : '1.6.4.3',
    'name' : { 'en': 'Animal sounds' },
    'description' : 'Use this domain for the sounds animals make. It is necessary to think through the sounds each type of animal makes, especially the important ones.',
    'value' : '1.6.4.3 Animal sounds'
  },
  {
    'guid' : '17d5f429-6550-4a3a-a755-5ac3c3d7e04f',
    'id' : '1.6.5',
    'code' : '1.6.5',
    'abbr' : '1.6.5',
    'name' : { 'en': 'Animal home' },
    'description' : 'Use this domain for animal homes. It is necessary to think through the homes of each type of animal, especially the important ones.',
    'value' : '1.6.5 Animal home'
  },
  {
    'guid' : '85646774-8145-4553-b8a7-6927cd077908',
    'id' : '1.6.6',
    'code' : '1.6.6',
    'abbr' : '1.6.6',
    'name' : { 'en': 'Animal group' },
    'description' : 'Use this domain for words referring to groups of animals.',
    'value' : '1.6.6 Animal group'
  },
  {
    'guid' : '7c64f65b-2889-4f90-ba61-6b5f7634d4bc',
    'id' : '1.6.7',
    'code' : '1.6.7',
    'abbr' : '1.6.7',
    'name' : { 'en': 'Male and female animals' },
    'description' : 'Use this domain for words referring to male and female animals. Most languages have special words for the male and female of a species only for domesticated animals. Sometimes there will be a word for the male and not the female, and vice versa (male dog, bitch). Sometimes the word for one is also used generically\n(cow for both female and generic).',
    'value' : '1.6.7 Male and female animals'
  },
  {
    'guid' : 'aa57936d-f8a9-4603-8c3d-27abccd13531',
    'id' : '1.7',
    'code' : '1.7',
    'abbr' : '1.7',
    'name' : { 'en': 'Nature, environment' },
    'description' : 'Use this domain for words referring to nature and the environment--the world around us. Include words that refer to how people damage or protect nature.',
    'value' : '1.7 Nature, environment'
  },
  {
    'guid' : 'f4ed1712-c072-4213-b89d-eb3a9be233b2',
    'id' : '1.7.1',
    'code' : '1.7.1',
    'abbr' : '1.7.1',
    'name' : { 'en': 'Natural' },
    'description' : 'Use this domain for words describing something that is natural--something in the world around us, as opposed to something that has been made or changed by people.',
    'value' : '1.7.1 Natural'
  },
  {
    'guid' : 'ba06de9e-63e1-43e6-ae94-77bea498379a',
    'id' : '2',
    'code' : '2',
    'abbr' : '2',
    'name' : { 'en': 'Person' },
    'description' : 'Use this domain for general words for a person or all mankind.',
    'value' : '2 Person'
  },
  {
    'guid' : '1b0270a5-babf-4151-99f5-279ba5a4b044',
    'id' : '2.1',
    'code' : '2.1',
    'abbr' : '2.1',
    'name' : { 'en': 'Body' },
    'description' : 'Use this domain for general words for the whole human body, and general words for any part of the body. Use a drawing or photo to label each part. Some words may be more general than others are and include some of the other words. For instance "head" is more general than "face" or "nose". Be sure that both general and specific parts are labeled.',
    'value' : '2.1 Body'
  },
  {
    'guid' : 'd98c1c67-b70e-4a35-89db-2e744bd5197f',
    'id' : '2.1.1',
    'code' : '2.1.1',
    'abbr' : '2.1.1',
    'name' : { 'en': 'Head' },
    'description' : 'Use this domain for the parts of the head.',
    'value' : '2.1.1 Head'
  },
  {
    'guid' : 'bc8d0ad4-6ebf-4fa0-bc7e-60e8ec9c43db',
    'id' : '2.1.1.1',
    'code' : '2.1.1.1',
    'abbr' : '2.1.1.1',
    'name' : { 'en': 'Eye' },
    'description' : 'Use this domain for words related to the eye.',
    'value' : '2.1.1.1 Eye'
  },
  {
    'guid' : '2e97b83d-1152-473f-9cbe-347f0655041a',
    'id' : '2.1.1.2',
    'code' : '2.1.1.2',
    'abbr' : '2.1.1.2',
    'name' : { 'en': 'Ear' },
    'description' : 'Use this domain for words related to the ear.',
    'value' : '2.1.1.2 Ear'
  },
  {
    'guid' : '68ed3e51-ddc4-4cec-89ff-605259ac9fcf',
    'id' : '2.1.1.3',
    'code' : '2.1.1.3',
    'abbr' : '2.1.1.3',
    'name' : { 'en': 'Nose' },
    'description' : 'Use this domain for words related to the nose.',
    'value' : '2.1.1.3 Nose'
  },
  {
    'guid' : 'e626c65e-eb79-4230-b07a-a6d975d3fe3d',
    'id' : '2.1.1.4',
    'code' : '2.1.1.4',
    'abbr' : '2.1.1.4',
    'name' : { 'en': 'Mouth' },
    'description' : 'Use this domain for words related to the mouth. Do not use this domain for words referring to eating, drinking, or speaking.',
    'value' : '2.1.1.4 Mouth'
  },
  {
    'guid' : 'c8602dc5-5c91-480a-b5ce-1c82fe3da83a',
    'id' : '2.1.1.5',
    'code' : '2.1.1.5',
    'abbr' : '2.1.1.5',
    'name' : { 'en': 'Tooth' },
    'description' : 'Use this domain for words related to the teeth.',
    'value' : '2.1.1.5 Tooth'
  },
  {
    'guid' : 'a80f12aa-c30d-4892-978a-b076985742d5',
    'id' : '2.1.2',
    'code' : '2.1.2',
    'abbr' : '2.1.2',
    'name' : { 'en': 'Torso' },
    'description' : 'Use this domain for the parts of the torso.',
    'value' : '2.1.2 Torso'
  },
  {
    'guid' : 'c5282457-be5f-4ce9-a802-91140cb0a22b',
    'id' : '2.1.3',
    'code' : '2.1.3',
    'abbr' : '2.1.3',
    'name' : { 'en': 'Limb' },
    'description' : 'Use this domain for words referring to a limb--either an arm or a leg.',
    'value' : '2.1.3 Limb'
  },
  {
    'guid' : 'c2f01aa8-9f94-43c9-9ada-b1e4a60aba07',
    'id' : '2.1.3.1',
    'code' : '2.1.3.1',
    'abbr' : '2.1.3.1',
    'name' : { 'en': 'Arm' },
    'description' : 'Use this domain for the parts of the arm.',
    'value' : '2.1.3.1 Arm'
  },
  {
    'guid' : '71f89512-17f0-484c-aca8-ddd226e3c794',
    'id' : '2.1.3.2',
    'code' : '2.1.3.2',
    'abbr' : '2.1.3.2',
    'name' : { 'en': 'Leg' },
    'description' : 'Use this domain for the parts of the leg and foot.',
    'value' : '2.1.3.2 Leg'
  },
  {
    'guid' : '9cfe4c5a-80d4-4b31-ba89-79b2e3d28a1c',
    'id' : '2.1.3.3',
    'code' : '2.1.3.3',
    'abbr' : '2.1.3.3',
    'name' : { 'en': 'Finger, toe' },
    'description' : 'Use this domain for words related to the fingers and toes.',
    'value' : '2.1.3.3 Finger, toe'
  },
  {
    'guid' : 'df2b9d7a-9b90-4704-8bc3-11dcffe985f4',
    'id' : '2.1.4',
    'code' : '2.1.4',
    'abbr' : '2.1.4',
    'name' : { 'en': 'Skin' },
    'description' : 'Use this domain for words related to the skin.',
    'value' : '2.1.4 Skin'
  },
  {
    'guid' : '2e5acfd2-3009-4496-9cc2-58d2a0088994',
    'id' : '2.1.5',
    'code' : '2.1.5',
    'abbr' : '2.1.5',
    'name' : { 'en': 'Hair' },
    'description' : 'Use this domain for words related to hair.',
    'value' : '2.1.5 Hair'
  },
  {
    'guid' : '1d8633e0-4279-4ddc-826e-16aa08a977e5',
    'id' : '2.1.6',
    'code' : '2.1.6',
    'abbr' : '2.1.6',
    'name' : { 'en': 'Bone, joint' },
    'description' : 'Use this domain for words related to the bones and joints.',
    'value' : '2.1.6 Bone, joint'
  },
  {
    'guid' : '49c525b3-2163-48e1-b3bd-57e5cdc486a4',
    'id' : '2.1.7',
    'code' : '2.1.7',
    'abbr' : '2.1.7',
    'name' : { 'en': 'Flesh' },
    'description' : 'Use this domain for words related to the soft tissue of the body.',
    'value' : '2.1.7 Flesh'
  },
  {
    'guid' : '4c862416-f7c4-4a3c-82ac-fe81e1efb879',
    'id' : '2.1.8',
    'code' : '2.1.8',
    'abbr' : '2.1.8',
    'name' : { 'en': 'Internal organs' },
    'description' : 'Use this domain for words referring to the internal organs.',
    'value' : '2.1.8 Internal organs'
  },
  {
    'guid' : '2d0b3058-d8bb-4110-a54a-e507b0d3a0e4',
    'id' : '2.1.8.1',
    'code' : '2.1.8.1',
    'abbr' : '2.1.8.1',
    'name' : { 'en': 'Heart' },
    'description' : 'Use this domain for functions of the heart and blood veins.',
    'value' : '2.1.8.1 Heart'
  },
  {
    'guid' : 'b9a4b336-080a-4973-a7e3-a9af10fc347c',
    'id' : '2.1.8.2',
    'code' : '2.1.8.2',
    'abbr' : '2.1.8.2',
    'name' : { 'en': 'Stomach' },
    'description' : 'Use this domain for words referring to the stomach and to the normal functions of the stomach. Do not use this domain for stomach illness.',
    'value' : '2.1.8.2 Stomach'
  },
  {
    'guid' : '7dbd7f43-4291-47f8-a392-6fdf3c98d522',
    'id' : '2.1.8.3',
    'code' : '2.1.8.3',
    'abbr' : '2.1.8.3',
    'name' : { 'en': 'Male organs' },
    'description' : 'Use this domain for words related to the male reproductive organs.',
    'value' : '2.1.8.3 Male organs'
  },
  {
    'guid' : '3d5d93ce-00e0-46ff-b220-553c12c38381',
    'id' : '2.1.8.4',
    'code' : '2.1.8.4',
    'abbr' : '2.1.8.4',
    'name' : { 'en': 'Female organs' },
    'description' : 'Use this domain for words related to the female reproductive organs and a woman"s monthly menstrual cycle. Some of these terms may be taboo. Care must be exercised in which terms are included in the dictionary. A group of women should decide which terms are "public" and can go in the dictionary, and which would be considered taboo, overly crude, or embarrassing. For example some societies have been afraid that their women will be taken advantage of if these terms are known.',
    'value' : '2.1.8.4 Female organs'
  },
  {
    'guid' : '7fe69c4c-2603-4949-afca-f39c010ad24e',
    'id' : '2.2',
    'code' : '2.2',
    'abbr' : '2.2',
    'name' : { 'en': 'Body functions' },
    'description' : 'Use this domain for the functions and actions of the whole body. Use the subdomains in this section  for functions, actions, secretions, and products of various parts of the body. In each domain include any special words that are used of animals.',
    'value' : '2.2 Body functions'
  },
  {
    'guid' : 'a1959b00-9702-4b45-ac46-93f18d3bc5e6',
    'id' : '2.2.1',
    'code' : '2.2.1',
    'abbr' : '2.2.1',
    'name' : { 'en': 'Breathe, breath' },
    'description' : 'Use this domain for words related to breathing.',
    'value' : '2.2.1 Breathe, breath'
  },
  {
    'guid' : '8b52a9d6-a07e-4ac8-8f84-6eb5ba578d97',
    'id' : '2.2.2',
    'code' : '2.2.2',
    'abbr' : '2.2.2',
    'name' : { 'en': 'Cough, sneeze' },
    'description' : 'Use this domain for words related to coughing, sneezing, and other actions of the mouth and nose.',
    'value' : '2.2.2 Cough, sneeze'
  },
  {
    'guid' : '75825d72-695b-4e92-9f33-0f3ab4d7dd11',
    'id' : '2.2.3',
    'code' : '2.2.3',
    'abbr' : '2.2.3',
    'name' : { 'en': 'Spit, saliva' },
    'description' : 'Use this domain for words related to spitting.',
    'value' : '2.2.3 Spit, saliva'
  },
  {
    'guid' : '591fd489-36e6-4ffd-a976-58876d851829',
    'id' : '2.2.4',
    'code' : '2.2.4',
    'abbr' : '2.2.4',
    'name' : { 'en': 'Mucus' },
    'description' : 'Use this domain for words related to mucus in the nose.',
    'value' : '2.2.4 Mucus'
  },
  {
    'guid' : '8c41d2d1-6da6-4ab8-8b29-7e226192d64e',
    'id' : '2.2.5',
    'code' : '2.2.5',
    'abbr' : '2.2.5',
    'name' : { 'en': 'Bleed, blood' },
    'description' : 'Use this domain for words related to blood and bleeding.',
    'value' : '2.2.5 Bleed, blood'
  },
  {
    'guid' : 'd395edc8-fb58-4ba4-8446-dacf8ea0477a',
    'id' : '2.2.6',
    'code' : '2.2.6',
    'abbr' : '2.2.6',
    'name' : { 'en': 'Sweat' },
    'description' : 'Use this domain for words related to sweating.',
    'value' : '2.2.6 Sweat'
  },
  {
    'guid' : 'e7f94aea-ba50-481d-b640-d5cd8bdedc72',
    'id' : '2.2.7',
    'code' : '2.2.7',
    'abbr' : '2.2.7',
    'name' : { 'en': 'Urinate, urine' },
    'description' : 'Use this domain for words related to urination.',
    'value' : '2.2.7 Urinate, urine'
  },
  {
    'guid' : 'cbc24a98-1c64-467e-98aa-251a28e4c0b8',
    'id' : '2.2.8',
    'code' : '2.2.8',
    'abbr' : '2.2.8',
    'name' : { 'en': 'Defecate, feces' },
    'description' : 'Use this domain for words related to defecation.',
    'value' : '2.2.8 Defecate, feces'
  },
  {
    'guid' : '38bbb33a-90bf-4a2c-a0e5-4bde7e134bd9',
    'id' : '2.3',
    'code' : '2.3',
    'abbr' : '2.3',
    'name' : { 'en': 'Sense, perceive' },
    'description' : 'Use this domain for general words related to all the senses--sight, hearing, smell, taste, and feeling. Some languages may not distinguish some of these senses, and some languages may have words for other senses. There are also other senses that animals have. If your language has words for other senses, include them here.',
    'value' : '2.3 Sense, perceive'
  },
  {
    'guid' : 'a8568a16-4c3b-4ce4-84a7-b8b0c6b0432f',
    'id' : '2.3.1',
    'code' : '2.3.1',
    'abbr' : '2.3.1',
    'name' : { 'en': 'See' },
    'description' : 'Use this domain for words related to seeing something (in general or without conscious choice).',
    'value' : '2.3.1 See'
  },
  {
    'guid' : 'd7861def-70c1-470f-bca6-8230cbbaa3e9',
    'id' : '2.3.1.1',
    'code' : '2.3.1.1',
    'abbr' : '2.3.1.1',
    'name' : { 'en': 'Look' },
    'description' : 'Use this domain for words that refer to looking at someone or something--to see something because you want to.',
    'value' : '2.3.1.1 Look'
  },
  {
    'guid' : 'fda0c1ac-5728-4ba2-9f8e-827f161b5bb1',
    'id' : '2.3.1.2',
    'code' : '2.3.1.2',
    'abbr' : '2.3.1.2',
    'name' : { 'en': 'Watch' },
    'description' : 'Use this domain for words that refer to watching someone or something--to look for some time at something that is happening because you are interested in it and want to know what is happening.',
    'value' : '2.3.1.2 Watch'
  },
  {
    'guid' : '90c06635-a12c-4cd4-a190-46925ff0f43e',
    'id' : '2.3.1.3',
    'code' : '2.3.1.3',
    'abbr' : '2.3.1.3',
    'name' : { 'en': 'Examine' },
    'description' : 'Use this domain for words referring to examining--to look carefully at something because you want to learn something about it.',
    'value' : '2.3.1.3 Examine'
  },
  {
    'guid' : '273f4956-f79f-4b1e-b552-466280a65e60',
    'id' : '2.3.1.4',
    'code' : '2.3.1.4',
    'abbr' : '2.3.1.4',
    'name' : { 'en': 'Show, let someone see' },
    'description' : 'Use this domain for words related to showing something to someone so that they can see it--to cause someone to see something.',
    'value' : '2.3.1.4 Show, let someone see'
  },
  {
    'guid' : '3f4c559f-ab4f-411f-a23b-d2396c977005',
    'id' : '2.3.1.5',
    'code' : '2.3.1.5',
    'abbr' : '2.3.1.5',
    'name' : { 'en': 'Visible' },
    'description' : 'Use this domain for words related to being able to see something--words that describe something that can be seen, something that cannot be seen, something that is easy to see, or something that is difficult to see.',
    'value' : '2.3.1.5 Visible'
  },
  {
    'guid' : '79a33505-0c33-4e92-89b9-6a42e6ff2228',
    'id' : '2.3.1.5.1',
    'code' : '2.3.1.5.1',
    'abbr' : '2.3.1.5.1',
    'name' : { 'en': 'Appear' },
    'description' : 'Use this domain for words referring to something appearing (becoming visible) and disappearing (becoming invisible).',
    'value' : '2.3.1.5.1 Appear'
  },
  {
    'guid' : 'cd6f1b37-5bdd-4237-8827-b1c947c8e1b4',
    'id' : '2.3.1.6',
    'code' : '2.3.1.6',
    'abbr' : '2.3.1.6',
    'name' : { 'en': 'Transparent' },
    'description' : 'Use this domain for words that describe how well you can see through something.',
    'value' : '2.3.1.6 Transparent'
  },
  {
    'guid' : 'ec118a28-fd23-48b3-8819-bfe1329f028d',
    'id' : '2.3.1.7',
    'code' : '2.3.1.7',
    'abbr' : '2.3.1.7',
    'name' : { 'en': 'Reflect, mirror' },
    'description' : 'Use this domain for words related to reflecting light.',
    'value' : '2.3.1.7 Reflect, mirror'
  },
  {
    'guid' : '4275df2e-d4f6-461a-9279-39e0712dc082',
    'id' : '2.3.1.8',
    'code' : '2.3.1.8',
    'abbr' : '2.3.1.8',
    'name' : { 'en': 'Appearance' },
    'description' : 'Use this domain for words related to how something appears.',
    'value' : '2.3.1.8 Appearance'
  },
  {
    'guid' : 'e4517880-aa2d-4977-b55a-dcb0b6d1f533',
    'id' : '2.3.1.8.1',
    'code' : '2.3.1.8.1',
    'abbr' : '2.3.1.8.1',
    'name' : { 'en': 'Beautiful' },
    'description' : 'Use this domain for words describing someone or something that is beautiful--pleasing in appearance,.',
    'value' : '2.3.1.8.1 Beautiful'
  },
  {
    'guid' : '2f151c35-72e1-4665-bc05-6fc70a3ecff2',
    'id' : '2.3.1.8.2',
    'code' : '2.3.1.8.2',
    'abbr' : '2.3.1.8.2',
    'name' : { 'en': 'Ugly' },
    'description' : 'Use this domain for words describing someone or something that is ugly--not pleasing in appearance.',
    'value' : '2.3.1.8.2 Ugly'
  },
  {
    'guid' : 'e4e05724-01ec-4c61-90f0-b8658cc8ca51',
    'id' : '2.3.1.9',
    'code' : '2.3.1.9',
    'abbr' : '2.3.1.9',
    'name' : { 'en': 'Something used to see' },
    'description' : 'Use this domain for words referring to glasses and other things people use to help them see.',
    'value' : '2.3.1.9 Something used to see'
  },
  {
    'guid' : '8503660c-03af-49ee-86b6-525aab4da828',
    'id' : '2.3.2',
    'code' : '2.3.2',
    'abbr' : '2.3.2',
    'name' : { 'en': 'Hear' },
    'description' : 'Use this domain for words related to hearing something (in general or without conscious choice).',
    'value' : '2.3.2 Hear'
  },
  {
    'guid' : '0de28f92-c851-413c-bb6c-3ad21f5e267f',
    'id' : '2.3.2.1',
    'code' : '2.3.2.1',
    'abbr' : '2.3.2.1',
    'name' : { 'en': 'Listen' },
    'description' : 'Use this domain for words related to listening--to deliberately hear something.',
    'value' : '2.3.2.1 Listen'
  },
  {
    'guid' : 'acf5e294-d169-45c1-a9d3-960536e018cc',
    'id' : '2.3.2.2',
    'code' : '2.3.2.2',
    'abbr' : '2.3.2.2',
    'name' : { 'en': 'Sound' },
    'description' : 'Use this domain for words referring to sounds.',
    'value' : '2.3.2.2 Sound'
  },
  {
    'guid' : 'fd33670e-ef16-4566-a62e-aa077e58407b',
    'id' : '2.3.2.3',
    'code' : '2.3.2.3',
    'abbr' : '2.3.2.3',
    'name' : { 'en': 'Types of sounds' },
    'description' : 'Use this domain for words referring to types of sounds.',
    'value' : '2.3.2.3 Types of sounds'
  },
  {
    'guid' : '6804db44-b71b-4452-98b1-b726bc7cf022',
    'id' : '2.3.2.4',
    'code' : '2.3.2.4',
    'abbr' : '2.3.2.4',
    'name' : { 'en': 'Loud' },
    'description' : 'Use this domain for words that describe loud sounds.',
    'value' : '2.3.2.4 Loud'
  },
  {
    'guid' : 'd853597b-f3ed-470b-b6dd-8fe93b8e43eb',
    'id' : '2.3.2.5',
    'code' : '2.3.2.5',
    'abbr' : '2.3.2.5',
    'name' : { 'en': 'Quiet' },
    'description' : 'Use this domain for words that describe quiet sounds.',
    'value' : '2.3.2.5 Quiet'
  },
  {
    'guid' : '8497fb66-8b91-46b9-a0d5-fb9385319561',
    'id' : '2.3.3',
    'code' : '2.3.3',
    'abbr' : '2.3.3',
    'name' : { 'en': 'Taste' },
    'description' : 'Use this domain for words related to tasting something.',
    'value' : '2.3.3 Taste'
  },
  {
    'guid' : 'ed7930df-e7b4-43c9-a11a-b09521276b57',
    'id' : '2.3.4',
    'code' : '2.3.4',
    'abbr' : '2.3.4',
    'name' : { 'en': 'Smell' },
    'description' : 'Use this domain for words related to smelling something.',
    'value' : '2.3.4 Smell'
  },
  {
    'guid' : '72274e9d-5d3c-4ae7-93ab-db3617cdda1e',
    'id' : '2.3.5',
    'code' : '2.3.5',
    'abbr' : '2.3.5',
    'name' : { 'en': 'Sense of touch' },
    'description' : 'Use this domain for words related to the sense of touch--to feel something with your skin, to feel hot or cold, to feel tired or rested.',
    'value' : '2.3.5 Sense of touch'
  },
  {
    'guid' : '295dc021-5b50-47b3-8340-1631c6d6fadc',
    'id' : '2.3.5.1',
    'code' : '2.3.5.1',
    'abbr' : '2.3.5.1',
    'name' : { 'en': 'Comfortable' },
    'description' : 'Use this domain for words related to feeling comfortable--to feel good in your body because there is nothing around you that makes you feel bad. This includes comfortable clothes, chair, bed, temperature, or journey.',
    'value' : '2.3.5.1 Comfortable'
  },
  {
    'guid' : 'f7706644-542f-4fcb-b8e1-e91d04c8032a',
    'id' : '2.4',
    'code' : '2.4',
    'abbr' : '2.4',
    'name' : { 'en': 'Body condition' },
    'description' : 'Use this domain for general words related to the condition of the body.',
    'value' : '2.4 Body condition'
  },
  {
    'guid' : 'e949f393-2a5b-4792-af8f-75138322ceee',
    'id' : '2.4.1',
    'code' : '2.4.1',
    'abbr' : '2.4.1',
    'name' : { 'en': 'Strong' },
    'description' : 'Use this domain for words that related to being strong, such as being able to lift a heavy object or being able to work hard.',
    'value' : '2.4.1 Strong'
  },
  {
    'guid' : '94af09fa-ff23-433b-a881-ceaca87d9d18',
    'id' : '2.4.2',
    'code' : '2.4.2',
    'abbr' : '2.4.2',
    'name' : { 'en': 'Weak' },
    'description' : 'Use this domain for words related to being weak.',
    'value' : '2.4.2 Weak'
  },
  {
    'guid' : '2daede19-ce5f-46b6-ae68-32d6092441f1',
    'id' : '2.4.3',
    'code' : '2.4.3',
    'abbr' : '2.4.3',
    'name' : { 'en': 'Energetic' },
    'description' : 'Use this domain for words related to being energetic.',
    'value' : '2.4.3 Energetic'
  },
  {
    'guid' : 'b5700ad7-36a1-4608-8789-8f84007244f8',
    'id' : '2.4.4',
    'code' : '2.4.4',
    'abbr' : '2.4.4',
    'name' : { 'en': 'Tired' },
    'description' : 'Use this domain for words related to being tired.',
    'value' : '2.4.4 Tired'
  },
  {
    'guid' : '2c401e7f-6ce9-470f-b6b6-fadf7a798536',
    'id' : '2.4.5',
    'code' : '2.4.5',
    'abbr' : '2.4.5',
    'name' : { 'en': 'Rest' },
    'description' : 'Use this domain for words related to resting.',
    'value' : '2.4.5 Rest'
  },
  {
    'guid' : '32bebe7e-bdcc-4e40-8f0a-894cd6b26f25',
    'id' : '2.5',
    'code' : '2.5',
    'abbr' : '2.5',
    'name' : { 'en': 'Healthy' },
    'description' : 'Use this domain for words related to a person being healthy--not sick.',
    'value' : '2.5 Healthy'
  },
  {
    'guid' : '7c6cad26-79c3-403a-a3aa-59babdfcd46f',
    'id' : '2.5.1',
    'code' : '2.5.1',
    'abbr' : '2.5.1',
    'name' : { 'en': 'Sick' },
    'description' : 'Use this domain for words describing a person who is sick.',
    'value' : '2.5.1 Sick'
  },
  {
    'guid' : '104d40c9-2a4f-4696-ad99-5cf0eb86ab2e',
    'id' : '2.5.1.1',
    'code' : '2.5.1.1',
    'abbr' : '2.5.1.1',
    'name' : { 'en': 'Recover from sickness' },
    'description' : 'Use this domain for words referring to recovering from sickness or injury.',
    'value' : '2.5.1.1 Recover from sickness'
  },
  {
    'guid' : 'cf337287-c9fa-43d2-93c4-284f45e262c0',
    'id' : '2.5.2',
    'code' : '2.5.2',
    'abbr' : '2.5.2',
    'name' : { 'en': 'Disease' },
    'description' : 'Use this domain for general words for disease and for words referring to specific diseases.',
    'value' : '2.5.2 Disease'
  },
  {
    'guid' : 'f0de6c5a-3df6-4483-8c63-2d8fcd6c97be',
    'id' : '2.5.2.1',
    'code' : '2.5.2.1',
    'abbr' : '2.5.2.1',
    'name' : { 'en': 'Malnutrition, starvation' },
    'description' : 'Use this domain for words related to not having enough food.',
    'value' : '2.5.2.1 Malnutrition, starvation'
  },
  {
    'guid' : '4d2a67fb-91c8-4436-87f4-f4eab6cb0828',
    'id' : '2.5.2.2',
    'code' : '2.5.2.2',
    'abbr' : '2.5.2.2',
    'name' : { 'en': 'Skin disease' },
    'description' : 'Use this domain for words related to skin diseases such as leprosy, boils, and rashes.',
    'value' : '2.5.2.2 Skin disease'
  },
  {
    'guid' : '39dcb6b9-94df-45be-a128-c14c7a9dcdbd',
    'id' : '2.5.2.3',
    'code' : '2.5.2.3',
    'abbr' : '2.5.2.3',
    'name' : { 'en': 'Stomach illness' },
    'description' : 'Use this domain for words related to stomach illness.',
    'value' : '2.5.2.3 Stomach illness'
  },
  {
    'guid' : '77f0d0dc-61ee-4373-9879-35a7059bd892',
    'id' : '2.5.2.4',
    'code' : '2.5.2.4',
    'abbr' : '2.5.2.4',
    'name' : { 'en': 'Tooth decay' },
    'description' : 'Use this domain for words related to tooth decay.',
    'value' : '2.5.2.4 Tooth decay'
  },
  {
    'guid' : '9d865347-6656-4ab7-8613-bf2e8bc53aa7',
    'id' : '2.5.3',
    'code' : '2.5.3',
    'abbr' : '2.5.3',
    'name' : { 'en': 'Injure' },
    'description' : 'Use this domain for words related to injuring someone.',
    'value' : '2.5.3 Injure'
  },
  {
    'guid' : '5fcadae4-b4a8-4600-8d30-c4f67986d619',
    'id' : '2.5.3.1',
    'code' : '2.5.3.1',
    'abbr' : '2.5.3.1',
    'name' : { 'en': 'Amputate' },
    'description' : 'Use this domain for words related to amputating or losing a limb or other part of your body.',
    'value' : '2.5.3.1 Amputate'
  },
  {
    'guid' : '962941b2-66bd-437f-aadc-b1921bcae5b4',
    'id' : '2.5.3.2',
    'code' : '2.5.3.2',
    'abbr' : '2.5.3.2',
    'name' : { 'en': 'Poison' },
    'description' : 'Use this domain for words referring to poison--something that is bad for your body if you eat it, it gets on you, or an animal injects it into you.',
    'value' : '2.5.3.2 Poison'
  },
  {
    'guid' : 'c2dbe83a-d638-45ac-a6d5-5f041b9dde71',
    'id' : '2.5.4',
    'code' : '2.5.4',
    'abbr' : '2.5.4',
    'name' : { 'en': 'Disabled' },
    'description' : 'Use this domain for general words for being disabled--to be injured or born with a condition, so that some part of your body does not work.',
    'value' : '2.5.4 Disabled'
  },
  {
    'guid' : 'b602c0e1-5398-4cc9-850b-7cfb5c592d13',
    'id' : '2.5.4.1',
    'code' : '2.5.4.1',
    'abbr' : '2.5.4.1',
    'name' : { 'en': 'Blind' },
    'description' : 'Use this domain for words related to being blind.',
    'value' : '2.5.4.1 Blind'
  },
  {
    'guid' : '23fb1571-c04e-4850-b499-f170bc45247f',
    'id' : '2.5.4.2',
    'code' : '2.5.4.2',
    'abbr' : '2.5.4.2',
    'name' : { 'en': 'Poor eyesight' },
    'description' : 'Use this domain for words related to having poor eyesight.',
    'value' : '2.5.4.2 Poor eyesight'
  },
  {
    'guid' : '3be7e3fe-89d4-471a-92bd-8c70fcb146bb',
    'id' : '2.5.4.3',
    'code' : '2.5.4.3',
    'abbr' : '2.5.4.3',
    'name' : { 'en': 'Deaf' },
    'description' : 'Use this domain for words related to being deaf.',
    'value' : '2.5.4.3 Deaf'
  },
  {
    'guid' : 'd2f05cc8-1a3f-4bc2-9a2b-38174bb84091',
    'id' : '2.5.4.4',
    'code' : '2.5.4.4',
    'abbr' : '2.5.4.4',
    'name' : { 'en': 'Mute' },
    'description' : 'Use this domain for words related to being mute--unable to speak (usually because of being unable to hear).',
    'value' : '2.5.4.4 Mute'
  },
  {
    'guid' : '4a5c8fdb-c8a0-49d2-a0d6-342428682d65',
    'id' : '2.5.4.5',
    'code' : '2.5.4.5',
    'abbr' : '2.5.4.5',
    'name' : { 'en': 'Birth defect' },
    'description' : 'Use this domain for words related to having a birth defect.',
    'value' : '2.5.4.5 Birth defect'
  },
  {
    'guid' : 'fa32115e-e389-47bd-91e1-61779172ccf2',
    'id' : '2.5.5',
    'code' : '2.5.5',
    'abbr' : '2.5.5',
    'name' : { 'en': 'Cause of disease' },
    'description' : 'Use this domain for words referring to the cause of disease.',
    'value' : '2.5.5 Cause of disease'
  },
  {
    'guid' : 'a894d991-d5da-45a6-9c62-009133257f36',
    'id' : '2.5.6',
    'code' : '2.5.6',
    'abbr' : '2.5.6',
    'name' : { 'en': 'Symptom of disease' },
    'description' : 'Use this domain for words for symptoms of disease--something that happens to you when you get sick, something that shows that you are sick.',
    'value' : '2.5.6 Symptom of disease'
  },
  {
    'guid' : '768aed05-dbc9-4caf-9461-76cb3720f908',
    'id' : '2.5.6.1',
    'code' : '2.5.6.1',
    'abbr' : '2.5.6.1',
    'name' : { 'en': 'Pain' },
    'description' : 'Use this domain for words related to pain',
    'value' : '2.5.6.1 Pain'
  },
  {
    'guid' : '5238fe9c-4bbe-444c-b5f6-18f946b3d6aa',
    'id' : '2.5.6.2',
    'code' : '2.5.6.2',
    'abbr' : '2.5.6.2',
    'name' : { 'en': 'Fever' },
    'description' : 'Use this domain for words related to having a fever.',
    'value' : '2.5.6.2 Fever'
  },
  {
    'guid' : '6eaba0c3-cdfe-435d-8811-7f2ddf6facbd',
    'id' : '2.5.6.3',
    'code' : '2.5.6.3',
    'abbr' : '2.5.6.3',
    'name' : { 'en': 'Swell' },
    'description' : 'Use this domain for words related to swelling of the body.',
    'value' : '2.5.6.3 Swell'
  },
  {
    'guid' : 'd83ebe2c-c1d6-49ec-a4a9-1cdced843387',
    'id' : '2.5.6.4',
    'code' : '2.5.6.4',
    'abbr' : '2.5.6.4',
    'name' : { 'en': 'Lose consciousness' },
    'description' : 'Use this domain for words related to losing consciousness, including fainting, being knocked out, and anesthesia.',
    'value' : '2.5.6.4 Lose consciousness'
  },
  {
    'guid' : '81e03df2-33a8-4735-aa23-80ef1c63679e',
    'id' : '2.5.6.5',
    'code' : '2.5.6.5',
    'abbr' : '2.5.6.5',
    'name' : { 'en': 'Dazed, confused' },
    'description' : 'Use this domain for words that describe the state of the mind when a person"s mind is not working well or when he is not thinking very well.',
    'value' : '2.5.6.5 Dazed, confused'
  },
  {
    'guid' : '5c091d8d-5bc4-40c3-8a30-2a08fb0794b8',
    'id' : '2.5.6.6',
    'code' : '2.5.6.6',
    'abbr' : '2.5.6.6',
    'name' : { 'en': 'Vision, hallucination' },
    'description' : 'Use this domain for words related to having a vision--when a person sees something that isn"t there because something unusual has happened to their mind. Include unusual, abnormal, and paranormal states of consciousness, visions, hallucinations, and spiritually induced trances.',
    'value' : '2.5.6.6 Vision, hallucination'
  },
  {
    'guid' : '101c16f8-ec76-4ec7-895a-fd814fef51dd',
    'id' : '2.5.7',
    'code' : '2.5.7',
    'abbr' : '2.5.7',
    'name' : { 'en': 'Treat disease' },
    'description' : 'Use this domain for words related to the treatment of disease and injury.',
    'value' : '2.5.7 Treat disease'
  },
  {
    'guid' : 'c01bbcef-7d89-4753-bafd-3a7f23648982',
    'id' : '2.5.7.1',
    'code' : '2.5.7.1',
    'abbr' : '2.5.7.1',
    'name' : { 'en': 'Doctor, nurse' },
    'description' : 'Use this domain for words referring to people who habitually take care of the sick and injured, such as those who do it for a living.',
    'value' : '2.5.7.1 Doctor, nurse'
  },
  {
    'guid' : 'b82c7ba0-9f4e-44da-bcd0-d30f5b224de5',
    'id' : '2.5.7.2',
    'code' : '2.5.7.2',
    'abbr' : '2.5.7.2',
    'name' : { 'en': 'Medicine' },
    'description' : 'Use this domain for words related to medicine, types of medicine, and the application of medicine.',
    'value' : '2.5.7.2 Medicine'
  },
  {
    'guid' : 'f3627c41-5daf-4f73-ac42-8a0522035e0b',
    'id' : '2.5.7.3',
    'code' : '2.5.7.3',
    'abbr' : '2.5.7.3',
    'name' : { 'en': 'Medicinal plants' },
    'description' : 'Use this domain for plants that are used for medicine.',
    'value' : '2.5.7.3 Medicinal plants'
  },
  {
    'guid' : '250a52e4-ede0-427d-8382-46a5742d4f96',
    'id' : '2.5.7.4',
    'code' : '2.5.7.4',
    'abbr' : '2.5.7.4',
    'name' : { 'en': 'Hospital' },
    'description' : 'Use this domain for words that refer to a place where the sick and injured are treated.',
    'value' : '2.5.7.4 Hospital'
  },
  {
    'guid' : '8e88ed6a-000d-400a-8cd8-7b3cc7f1818c',
    'id' : '2.5.7.5',
    'code' : '2.5.7.5',
    'abbr' : '2.5.7.5',
    'name' : { 'en': 'Traditional medicine' },
    'description' : 'Use this domain for words related to traditional medicine. There may be no distinction in terminology between "modern medicine" and "traditional medicine." In that case this domain should be ignored. (Our purpose here is not to judge the value of traditional medicine, but to collect and describe the words used for it.)',
    'value' : '2.5.7.5 Traditional medicine'
  },
  {
    'guid' : 'a01a1900-fc1f-462e-ba3d-ae822711b034',
    'id' : '2.5.8',
    'code' : '2.5.8',
    'abbr' : '2.5.8',
    'name' : { 'en': 'Mental illness' },
    'description' : 'Use this domain for words related to being mentally ill or disabled.',
    'value' : '2.5.8 Mental illness'
  },
  {
    'guid' : '50db27b5-89eb-4ffb-af82-566f51c8ec0b',
    'id' : '2.6',
    'code' : '2.6',
    'abbr' : '2.6',
    'name' : { 'en': 'Life' },
    'description' : 'Use this domain for general words referring to being alive and to a person"s lifetime.',
    'value' : '2.6 Life'
  },
  {
    'guid' : 'ffcc57f8-6c6d-4bf4-85be-9220ca7c739d',
    'id' : '2.6.1',
    'code' : '2.6.1',
    'abbr' : '2.6.1',
    'name' : { 'en': 'Marriage' },
    'description' : 'Use this domain for words related to the state of being married.',
    'value' : '2.6.1 Marriage'
  },
  {
    'guid' : '2e95bd1e-82f0-461d-8ca6-b5f1ce1fb180',
    'id' : '2.6.1.1',
    'code' : '2.6.1.1',
    'abbr' : '2.6.1.1',
    'name' : { 'en': 'Arrange a marriage' },
    'description' : 'Use this domain for all the words related to arranging a marriage. Cultures vary widely in their practices. In some cultures marriages are arranged by the parents. In other cultures a man must seek a wife for himself. Some cultures allow either practice or a combination of the two. So some of the questions below may be inappropriate to your culture.',
    'value' : '2.6.1.1 Arrange a marriage'
  },
  {
    'guid' : 'ed4a2ca6-c03c-4c72-9431-b72fb7294b8f',
    'id' : '2.6.1.2',
    'code' : '2.6.1.2',
    'abbr' : '2.6.1.2',
    'name' : { 'en': 'Wedding' },
    'description' : 'Use this domain for words related to the wedding ceremony.',
    'value' : '2.6.1.2 Wedding'
  },
  {
    'guid' : 'f7e625a6-53e3-4f9b-8764-119e3906f5cf',
    'id' : '2.6.1.3',
    'code' : '2.6.1.3',
    'abbr' : '2.6.1.3',
    'name' : { 'en': 'Unmarried' },
    'description' : 'Use this domain for words related to being unmarried.',
    'value' : '2.6.1.3 Unmarried'
  },
  {
    'guid' : 'f6e416b3-50b1-4e48-8a39-2998725b1c79',
    'id' : '2.6.1.4',
    'code' : '2.6.1.4',
    'abbr' : '2.6.1.4',
    'name' : { 'en': 'Divorce' },
    'description' : 'Use this domain for words related to divorce--to legally end your marriage.',
    'value' : '2.6.1.4 Divorce'
  },
  {
    'guid' : 'edbfc928-049c-4cb7-8c88-8e8af38287c7',
    'id' : '2.6.1.5',
    'code' : '2.6.1.5',
    'abbr' : '2.6.1.5',
    'name' : { 'en': 'Romantic love' },
    'description' : 'Use this domain for words related to romantic love.',
    'value' : '2.6.1.5 Romantic love'
  },
  {
    'guid' : 'c753fc8b-22ae-4e71-807f-56fb3ebd3cdd',
    'id' : '2.6.2',
    'code' : '2.6.2',
    'abbr' : '2.6.2',
    'name' : { 'en': 'Sexual relations' },
    'description' : 'Use this domain for words related to sexual relations and having sex. Be careful that your domain label does not use a taboo word.',
    'value' : '2.6.2 Sexual relations'
  },
  {
    'guid' : '9f6754ae-a429-4bba-8f6f-2cd4ea0cbe45',
    'id' : '2.6.2.1',
    'code' : '2.6.2.1',
    'abbr' : '2.6.2.1',
    'name' : { 'en': 'Virginity' },
    'description' : 'Use this domain for words related to being a virgin--a person who has never had sex.',
    'value' : '2.6.2.1 Virginity'
  },
  {
    'guid' : '23190f9e-2db2-4ef9-8c0e-495dbef05571',
    'id' : '2.6.2.2',
    'code' : '2.6.2.2',
    'abbr' : '2.6.2.2',
    'name' : { 'en': 'Attract sexually' },
    'description' : 'Use this domain for words related to attracting someone sexually--to cause someone to want to have sex with you, and for words related to being sexually attracted to someone--to want to have sex with someone.',
    'value' : '2.6.2.2 Attract sexually'
  },
  {
    'guid' : 'b0905fa9-2f90-410b-8eb5-7c7944c4f0f9',
    'id' : '2.6.2.3',
    'code' : '2.6.2.3',
    'abbr' : '2.6.2.3',
    'name' : { 'en': 'Sexual immorality' },
    'description' : 'Use this domain for words referring to illicit sexual relations.',
    'value' : '2.6.2.3 Sexual immorality'
  },
  {
    'guid' : '35e61ec4-0542-4583-b5da-0aa5e31a35aa',
    'id' : '2.6.3',
    'code' : '2.6.3',
    'abbr' : '2.6.3',
    'name' : { 'en': 'Birth' },
    'description' : 'Use this domain for words related to giving birth and being born.',
    'value' : '2.6.3 Birth'
  },
  {
    'guid' : '3cb4c07c-8760-4ff9-8d45-1c0bed80ffb3',
    'id' : '2.6.3.1',
    'code' : '2.6.3.1',
    'abbr' : '2.6.3.1',
    'name' : { 'en': 'Pregnancy' },
    'description' : 'Use this domain for words related to being pregnant.',
    'value' : '2.6.3.1 Pregnancy'
  },
  {
    'guid' : 'bd002dfa-e842-47d6-b11b-3c213cbf133a',
    'id' : '2.6.3.2',
    'code' : '2.6.3.2',
    'abbr' : '2.6.3.2',
    'name' : { 'en': 'Fetus' },
    'description' : 'Use this domain for words related to a fetus--a baby that has not been born yet.',
    'value' : '2.6.3.2 Fetus'
  },
  {
    'guid' : 'dbebc3bd-2d01-4d62-a009-866c18ee3527',
    'id' : '2.6.3.3',
    'code' : '2.6.3.3',
    'abbr' : '2.6.3.3',
    'name' : { 'en': 'Miscarriage' },
    'description' : 'Use this domain for words related to the prevention or termination of a pregnancy, and for words related to killing babies.',
    'value' : '2.6.3.3 Miscarriage'
  },
  {
    'guid' : '56984b2b-3417-49b4-a082-1a383551a9e9',
    'id' : '2.6.3.4',
    'code' : '2.6.3.4',
    'abbr' : '2.6.3.4',
    'name' : { 'en': 'Labor and birth pains' },
    'description' : 'Use this domain for words related to labor and birth pains.',
    'value' : '2.6.3.4 Labor and birth pains'
  },
  {
    'guid' : '5c468a85-e45f-4ea0-a3ba-68feda7e85a1',
    'id' : '2.6.3.5',
    'code' : '2.6.3.5',
    'abbr' : '2.6.3.5',
    'name' : { 'en': 'Help to give birth' },
    'description' : 'Use this domain for words related to helping a woman to give birth.',
    'value' : '2.6.3.5 Help to give birth'
  },
  {
    'guid' : 'f3d162d7-da79-4ce4-9610-040f03b57d9d',
    'id' : '2.6.3.6',
    'code' : '2.6.3.6',
    'abbr' : '2.6.3.6',
    'name' : { 'en': 'Unusual birth' },
    'description' : 'Use this domain for words related to an unusual birth.',
    'value' : '2.6.3.6 Unusual birth'
  },
  {
    'guid' : 'f4f99472-0b23-42b9-8b51-1d56fe24715b',
    'id' : '2.6.3.7',
    'code' : '2.6.3.7',
    'abbr' : '2.6.3.7',
    'name' : { 'en': 'Multiple births' },
    'description' : 'Use this domain for words related to multiple births--when a woman give birth to more than one baby at the same time.',
    'value' : '2.6.3.7 Multiple births'
  },
  {
    'guid' : '447f258b-2160-42c7-9431-ffeeb86edcb8',
    'id' : '2.6.3.8',
    'code' : '2.6.3.8',
    'abbr' : '2.6.3.8',
    'name' : { 'en': 'Fertile, infertile' },
    'description' : 'Use this domain for words related to being unable to have children.',
    'value' : '2.6.3.8 Fertile, infertile'
  },
  {
    'guid' : '34a02a17-23fb-4260-9f97-c125842a3594',
    'id' : '2.6.3.9',
    'code' : '2.6.3.9',
    'abbr' : '2.6.3.9',
    'name' : { 'en': 'Birth ceremony' },
    'description' : 'Use this domain for words related to a birth ceremony.',
    'value' : '2.6.3.9 Birth ceremony'
  },
  {
    'guid' : '444407f2-0c75-4bb9-a84c-cbd52d0fa9c9',
    'id' : '2.6.4',
    'code' : '2.6.4',
    'abbr' : '2.6.4',
    'name' : { 'en': 'Stage of life' },
    'description' : 'Use this domain for words referring to a stage of life--a time period in a person"s life.',
    'value' : '2.6.4 Stage of life'
  },
  {
    'guid' : '79ca34ea-68a7-4fe9-b5ac-4f3d6a1ab99d',
    'id' : '2.6.4.1',
    'code' : '2.6.4.1',
    'abbr' : '2.6.4.1',
    'name' : { 'en': 'Baby' },
    'description' : 'Use this domain for words related to a baby.',
    'value' : '2.6.4.1 Baby'
  },
  {
    'guid' : 'a19e219a-6cc1-4057-a8d9-18554ae88de1',
    'id' : '2.6.4.1.1',
    'code' : '2.6.4.1.1',
    'abbr' : '2.6.4.1.1',
    'name' : { 'en': 'Care for a baby' },
    'description' : 'Use this domain for words related to caring for a baby.',
    'value' : '2.6.4.1.1 Care for a baby'
  },
  {
    'guid' : '7d472dd5-636d-4499-bf66-83cf23c0dbe1',
    'id' : '2.6.4.2',
    'code' : '2.6.4.2',
    'abbr' : '2.6.4.2',
    'name' : { 'en': 'Child' },
    'description' : 'Use this domain for words related to a child.',
    'value' : '2.6.4.2 Child'
  },
  {
    'guid' : 'ca91e41a-81c3-4c96-87e6-f67477fcd686',
    'id' : '2.6.4.2.1',
    'code' : '2.6.4.2.1',
    'abbr' : '2.6.4.2.1',
    'name' : { 'en': 'Rear a child' },
    'description' : 'Use this domain for words related to rearing a child--to take care of someone while they are a child so that their needs are met and they become a good person.',
    'value' : '2.6.4.2.1 Rear a child'
  },
  {
    'guid' : 'f74f28d1-8742-4c9f-95dc-d08336e91249',
    'id' : '2.6.4.3',
    'code' : '2.6.4.3',
    'abbr' : '2.6.4.3',
    'name' : { 'en': 'Youth' },
    'description' : 'Use this domain for words referring to a youth.',
    'value' : '2.6.4.3 Youth'
  },
  {
    'guid' : 'fc82fcec-d03c-4fb0-bf62-714c71754402',
    'id' : '2.6.4.4',
    'code' : '2.6.4.4',
    'abbr' : '2.6.4.4',
    'name' : { 'en': 'Adult' },
    'description' : 'Use this domain for words referring to an adult.',
    'value' : '2.6.4.4 Adult'
  },
  {
    'guid' : '8fff393a-23c2-42bb-8da8-9b151e790904',
    'id' : '2.6.4.5',
    'code' : '2.6.4.5',
    'abbr' : '2.6.4.5',
    'name' : { 'en': 'Old person' },
    'description' : 'Use this domain for words related to old age and older persons?',
    'value' : '2.6.4.5 Old person'
  },
  {
    'guid' : 'f718fc15-59b2-4b6a-a9e3-39b3e8d487d7',
    'id' : '2.6.4.6',
    'code' : '2.6.4.6',
    'abbr' : '2.6.4.6',
    'name' : { 'en': 'Grow, get bigger' },
    'description' : 'Use this domain for words referring to people, animals, or plants growing and getting bigger.',
    'value' : '2.6.4.6 Grow, get bigger'
  },
  {
    'guid' : 'adf6ad2b-7af9-4bd8-a7b4-f864b9dad86d',
    'id' : '2.6.4.7',
    'code' : '2.6.4.7',
    'abbr' : '2.6.4.7',
    'name' : { 'en': 'Initiation' },
    'description' : 'Use this domain for words related to initiation rites--a ceremony when a child becomes an adult.',
    'value' : '2.6.4.7 Initiation'
  },
  {
    'guid' : '9e587127-4f2c-4796-9c67-d37332b57303',
    'id' : '2.6.4.8',
    'code' : '2.6.4.8',
    'abbr' : '2.6.4.8',
    'name' : { 'en': 'Peer group' },
    'description' : 'Use this domain for words referring to a peer group--all the people who were born during the same time period.',
    'value' : '2.6.4.8 Peer group'
  },
  {
    'guid' : '0db5817e-05bf-4703-a6b9-e239ac44f857',
    'id' : '2.6.5',
    'code' : '2.6.5',
    'abbr' : '2.6.5',
    'name' : { 'en': 'Male, female' },
    'description' : 'Use this domain for words referring to male and female people.',
    'value' : '2.6.5 Male, female'
  },
  {
    'guid' : '04582a28-b94a-4e7f-8cc4-5cdefa8a39f0',
    'id' : '2.6.5.1',
    'code' : '2.6.5.1',
    'abbr' : '2.6.5.1',
    'name' : { 'en': 'Man' },
    'description' : 'Use this domain for words referring to a man or any male person.',
    'value' : '2.6.5.1 Man'
  },
  {
    'guid' : 'ae6f73ab-432d-42e8-aa1a-c848652a13f0',
    'id' : '2.6.5.2',
    'code' : '2.6.5.2',
    'abbr' : '2.6.5.2',
    'name' : { 'en': 'Woman' },
    'description' : 'Use this domain for words referring to a woman or any female person.',
    'value' : '2.6.5.2 Woman'
  },
  {
    'guid' : '1c3c8af0-56b9-4617-862e-21f39b388606',
    'id' : '2.6.6',
    'code' : '2.6.6',
    'abbr' : '2.6.6',
    'name' : { 'en': 'Die' },
    'description' : 'Use this domain for words related to dying.',
    'value' : '2.6.6 Die'
  },
  {
    'guid' : 'b8d2fdb9-22ea-4040-8abb-aeeff0399f23',
    'id' : '2.6.6.1',
    'code' : '2.6.6.1',
    'abbr' : '2.6.6.1',
    'name' : { 'en': 'Kill' },
    'description' : 'Use this domain for words related to killing someone--to cause someone to die.',
    'value' : '2.6.6.1 Kill'
  },
  {
    'guid' : '7e2b6218-0837-4b16-a982-c9535cccdb21',
    'id' : '2.6.6.2',
    'code' : '2.6.6.2',
    'abbr' : '2.6.6.2',
    'name' : { 'en': 'Corpse' },
    'description' : 'Use this domain for words referring to a corpse--the body of a person who has died.',
    'value' : '2.6.6.2 Corpse'
  },
  {
    'guid' : '08d5e632-0aed-4924-b3bb-d43de3420385',
    'id' : '2.6.6.3',
    'code' : '2.6.6.3',
    'abbr' : '2.6.6.3',
    'name' : { 'en': 'Funeral' },
    'description' : 'Use this domain for words related to a funeral and other things that are done after a person dies.',
    'value' : '2.6.6.3 Funeral'
  },
  {
    'guid' : 'f2d0f288-5bbe-4fa0-9e8f-ddcc74891701',
    'id' : '2.6.6.4',
    'code' : '2.6.6.4',
    'abbr' : '2.6.6.4',
    'name' : { 'en': 'Mourn' },
    'description' : 'Use this domain for words related to mourning a death--to feel bad because someone died and to show this feeling in various ways. Include whatever cultural practices are used.',
    'value' : '2.6.6.4 Mourn'
  },
  {
    'guid' : 'ca9c215a-e568-4d09-b3a9-b5727cd831d6',
    'id' : '2.6.6.5',
    'code' : '2.6.6.5',
    'abbr' : '2.6.6.5',
    'name' : { 'en': 'Bury' },
    'description' : 'Use this domain for words related to disposing of a dead body. Different cultures have practices other than burying a body in the ground. Include words for all practices used by the culture.',
    'value' : '2.6.6.5 Bury'
  },
  {
    'guid' : '32cf3835-bced-4ea1-9c7a-f7ff653e59fe',
    'id' : '2.6.6.6',
    'code' : '2.6.6.6',
    'abbr' : '2.6.6.6',
    'name' : { 'en': 'Grave' },
    'description' : 'Use this domain for words related to a grave--the place where a dead body is put.',
    'value' : '2.6.6.6 Grave'
  },
  {
    'guid' : '1ec85151-eba0-48f4-b56d-4f8040602a4b',
    'id' : '2.6.6.7',
    'code' : '2.6.6.7',
    'abbr' : '2.6.6.7',
    'name' : { 'en': 'Inherit' },
    'description' : 'Use this domain for words related to inheriting something from your parents after they die.',
    'value' : '2.6.6.7 Inherit'
  },
  {
    'guid' : '0281fb1d-ab12-41b9-a3dc-09ef6b1e4733',
    'id' : '2.6.6.8',
    'code' : '2.6.6.8',
    'abbr' : '2.6.6.8',
    'name' : { 'en': 'Life after death' },
    'description' : 'Use this domain for words related to life after death.',
    'value' : '2.6.6.8 Life after death'
  },
  {
    'guid' : 'f4491f9b-3c5e-42ab-afc0-f22e19d0fff5',
    'id' : '3',
    'code' : '3',
    'abbr' : '3',
    'name' : { 'en': 'Language and thought' },
    'description' : 'Use this domain for general words referring to mental and verbal activity. This domain is primarily for grouping many related domains. Therefore there may be no general word in a language to cover such a broad area of meaning.',
    'value' : '3 Language and thought'
  },
  {
    'guid' : '1cb79293-d4f7-4990-9f50-3bb595744f61',
    'id' : '3.1',
    'code' : '3.1',
    'abbr' : '3.1',
    'name' : { 'en': 'Soul, spirit' },
    'description' : 'Use this domain for general words related to the immaterial, non-physical part of a person, as opposed to the body.',
    'value' : '3.1 Soul, spirit'
  },
  {
    'guid' : '6fe11b6a-8d01-4a0b-bdeb-e4e6f420340a',
    'id' : '3.1.1',
    'code' : '3.1.1',
    'abbr' : '3.1.1',
    'name' : { 'en': 'Personality' },
    'description' : 'Use this domain for words that describe a person"s personality (the way he usually thinks, talks, and how he acts with other people).',
    'value' : '3.1.1 Personality'
  },
  {
    'guid' : 'a2508183-7ea5-434e-a773-00d53087d27b',
    'id' : '3.1.2',
    'code' : '3.1.2',
    'abbr' : '3.1.2',
    'name' : { 'en': 'Mental state' },
    'description' : 'Use this domain for words referring to a person"s mental state.',
    'value' : '3.1.2 Mental state'
  },
  {
    'guid' : 'e9ef98d9-8844-4804-88a5-614493d150f5',
    'id' : '3.1.2.1',
    'code' : '3.1.2.1',
    'abbr' : '3.1.2.1',
    'name' : { 'en': 'Alert' },
    'description' : 'Use this domain for words referring to a mental state when the mind is working hard.',
    'value' : '3.1.2.1 Alert'
  },
  {
    'guid' : '267b98aa-e17c-4ebb-a752-ed4210701867',
    'id' : '3.1.2.2',
    'code' : '3.1.2.2',
    'abbr' : '3.1.2.2',
    'name' : { 'en': 'Notice' },
    'description' : 'Use this domain for words related to noticing something.',
    'value' : '3.1.2.2 Notice'
  },
  {
    'guid' : '5b5bcd42-09bb-4c2f-a2da-569cfa69b6ac',
    'id' : '3.1.2.3',
    'code' : '3.1.2.3',
    'abbr' : '3.1.2.3',
    'name' : { 'en': 'Attention' },
    'description' : 'Use this domain for words referring to a mental state when the mind is working hard.',
    'value' : '3.1.2.3 Attention'
  },
  {
    'guid' : '878a313b-a201-444e-8bdb-67048d60c63e',
    'id' : '3.1.2.4',
    'code' : '3.1.2.4',
    'abbr' : '3.1.2.4',
    'name' : { 'en': 'Ignore' },
    'description' : 'Use this domain for words related to ignoring someone--to not look at, listen to, or talk to someone because you think they are not important or you don"t like them.',
    'value' : '3.1.2.4 Ignore'
  },
  {
    'guid' : 'df9ee372-e92e-4f73-aac5-d36908497698',
    'id' : '3.2',
    'code' : '3.2',
    'abbr' : '3.2',
    'name' : { 'en': 'Think' },
    'description' : 'Use this domain for words related to thinking, thought processes, and kinds of thinking.',
    'value' : '3.2 Think'
  },
  {
    'guid' : '7185bd93-5281-46de-80af-767f0ec40ff6',
    'id' : '3.2.1',
    'code' : '3.2.1',
    'abbr' : '3.2.1',
    'name' : { 'en': 'Mind' },
    'description' : 'Use this domain for general words referring to the mind--the part of a person that thinks.',
    'value' : '3.2.1 Mind'
  },
  {
    'guid' : '2a2af155-9db9-41c5-860a-fe0a3a09d6de',
    'id' : '3.2.1.1',
    'code' : '3.2.1.1',
    'abbr' : '3.2.1.1',
    'name' : { 'en': 'Think about' },
    'description' : 'Use this domain for words related to thinking about something for some time.',
    'value' : '3.2.1.1 Think about'
  },
  {
    'guid' : 'be89e0ba-4c6a-4986-ac0d-859a901b89a1',
    'id' : '3.2.1.2',
    'code' : '3.2.1.2',
    'abbr' : '3.2.1.2',
    'name' : { 'en': 'Imagine' },
    'description' : 'Use this domain for imagining things--to think about something that does not exist, or to think about something happening that has never happened.',
    'value' : '3.2.1.2 Imagine'
  },
  {
    'guid' : '48ac206f-2706-4500-bb63-2e499b790259',
    'id' : '3.2.1.3',
    'code' : '3.2.1.3',
    'abbr' : '3.2.1.3',
    'name' : { 'en': 'Intelligent' },
    'description' : 'Use this domain for words that describe a person who thinks well.',
    'value' : '3.2.1.3 Intelligent'
  },
  {
    'guid' : '1f3519f8-d946-4857-a1fd-553d98dddf6d',
    'id' : '3.2.1.4',
    'code' : '3.2.1.4',
    'abbr' : '3.2.1.4',
    'name' : { 'en': 'Stupid' },
    'description' : 'Use this domain for words that describe a person who does not think well.',
    'value' : '3.2.1.4 Stupid'
  },
  {
    'guid' : '6866ee4c-78cd-47d1-ba4a-8461fdcc5e2a',
    'id' : '3.2.1.5',
    'code' : '3.2.1.5',
    'abbr' : '3.2.1.5',
    'name' : { 'en': 'Logical' },
    'description' : 'Use this domain for words describing logical thinking.',
    'value' : '3.2.1.5 Logical'
  },
  {
    'guid' : '3fd34185-19a1-44bd-8555-bc76e2847bee',
    'id' : '3.2.1.6',
    'code' : '3.2.1.6',
    'abbr' : '3.2.1.6',
    'name' : { 'en': 'Instinct' },
    'description' : 'Use this domain for words related to instinct--to know something without being told, to know what to do without taught how to do it.',
    'value' : '3.2.1.6 Instinct'
  },
  {
    'guid' : 'a42db5b4-6317-4d3f-beff-cb92dbaca914',
    'id' : '3.2.2',
    'code' : '3.2.2',
    'abbr' : '3.2.2',
    'name' : { 'en': 'Learn' },
    'description' : 'Use this domain for words referring to learning something, acquiring information, gaining knowledge (whether done intentionally or unintentionally), or discovering the answer to some question.',
    'value' : '3.2.2 Learn'
  },
  {
    'guid' : 'f0e68d2f-f7b3-4722-80a4-8e9c5638b0d4',
    'id' : '3.2.2.1',
    'code' : '3.2.2.1',
    'abbr' : '3.2.2.1',
    'name' : { 'en': 'Study' },
    'description' : 'Use this domain for words referring to studying--to try to learn something.',
    'value' : '3.2.2.1 Study'
  },
  {
    'guid' : 'f39b14c4-52cf-4afa-956c-f0f5815ef6ac',
    'id' : '3.2.2.2',
    'code' : '3.2.2.2',
    'abbr' : '3.2.2.2',
    'name' : { 'en': 'Check' },
    'description' : 'Use this domain for words referring to checking something--when you think something is true or correct, but you aren"t sure, you do something to find out if it is true or correct.',
    'value' : '3.2.2.2 Check'
  },
  {
    'guid' : '167bfba5-0785-4bb5-a083-3ffbefa57897',
    'id' : '3.2.2.3',
    'code' : '3.2.2.3',
    'abbr' : '3.2.2.3',
    'name' : { 'en': 'Evaluate, test' },
    'description' : 'Use this domain for words referring to the process of determining the truth or falsehood of something, or for determining the nature or value of something.',
    'value' : '3.2.2.3 Evaluate, test'
  },
  {
    'guid' : 'f1af3f4c-6e0e-4cfa-adcf-9dcddf05feab',
    'id' : '3.2.2.4',
    'code' : '3.2.2.4',
    'abbr' : '3.2.2.4',
    'name' : { 'en': 'Guess' },
    'description' : 'Use this domain for words referring to answering a question when one is unsure of the answer.',
    'value' : '3.2.2.4 Guess'
  },
  {
    'guid' : '909a3113-88bc-470b-8d48-7c0d37966982',
    'id' : '3.2.2.5',
    'code' : '3.2.2.5',
    'abbr' : '3.2.2.5',
    'name' : { 'en': 'Solve' },
    'description' : 'Use this domain for words related to solving something--finding the answer to something that is difficult to understand.',
    'value' : '3.2.2.5 Solve'
  },
  {
    'guid' : 'eac4b58e-1fd7-4ce1-9a68-c7516470e876',
    'id' : '3.2.2.6',
    'code' : '3.2.2.6',
    'abbr' : '3.2.2.6',
    'name' : { 'en': 'Realize' },
    'description' : 'Use this domain for words related to realizing something.',
    'value' : '3.2.2.6 Realize'
  },
  {
    'guid' : '880647e5-6543-46dd-9178-8edae9272add',
    'id' : '3.2.2.7',
    'code' : '3.2.2.7',
    'abbr' : '3.2.2.7',
    'name' : { 'en': 'Willing to learn' },
    'description' : 'Use this domain for words referring to being willing to learn or unwilling to learn.',
    'value' : '3.2.2.7 Willing to learn'
  },
  {
    'guid' : '07476166-c5e5-4701-97d3-d97de8b5be6f',
    'id' : '3.2.3',
    'code' : '3.2.3',
    'abbr' : '3.2.3',
    'name' : { 'en': 'Know' },
    'description' : 'Use this domain for words referring to the results of thinking.',
    'value' : '3.2.3 Know'
  },
  {
    'guid' : '14954a0f-5c8a-4680-90b0-53398bd3a2a7',
    'id' : '3.2.3.1',
    'code' : '3.2.3.1',
    'abbr' : '3.2.3.1',
    'name' : { 'en': 'Known, unknown' },
    'description' : 'Use this domain for words that describe whether or not something is known.',
    'value' : '3.2.3.1 Known, unknown'
  },
  {
    'guid' : 'e3300171-d7b2-4fb4-a103-e8fdcf3ff2ed',
    'id' : '3.2.3.2',
    'code' : '3.2.3.2',
    'abbr' : '3.2.3.2',
    'name' : { 'en': 'Area of knowledge' },
    'description' : 'Use this domain for words referring to an area of knowledge.',
    'value' : '3.2.3.2 Area of knowledge'
  },
  {
    'guid' : 'e0e8af5a-04c1-49a1-9955-9a2af7879068',
    'id' : '3.2.3.3',
    'code' : '3.2.3.3',
    'abbr' : '3.2.3.3',
    'name' : { 'en': 'Secret' },
    'description' : 'Use this domain for words that describe whether or not something is known.',
    'value' : '3.2.3.3 Secret'
  },
  {
    'guid' : 'd10301f3-573c-4005-ad65-1c73fb80b3b6',
    'id' : '3.2.4',
    'code' : '3.2.4',
    'abbr' : '3.2.4',
    'name' : { 'en': 'Understand' },
    'description' : 'Use this domain for words referring to understanding a topic or the meaning of something.',
    'value' : '3.2.4 Understand'
  },
  {
    'guid' : 'cab8e9dc-5e4f-4a12-8b3d-4acbb7ad2059',
    'id' : '3.2.4.1',
    'code' : '3.2.4.1',
    'abbr' : '3.2.4.1',
    'name' : { 'en': 'Misunderstand' },
    'description' : 'Use this domain for when a person does not understand a topic or the meaning of something.',
    'value' : '3.2.4.1 Misunderstand'
  },
  {
    'guid' : 'cba1f6cc-58ac-4d09-aa6a-1661f5945787',
    'id' : '3.2.4.2',
    'code' : '3.2.4.2',
    'abbr' : '3.2.4.2',
    'name' : { 'en': 'Understandable' },
    'description' : 'Use this domain for words that describe something that is easy to understand.',
    'value' : '3.2.4.2 Understandable'
  },
  {
    'guid' : '6ea13b69-b2a8-41f9-b0bc-14316ebc5118',
    'id' : '3.2.4.3',
    'code' : '3.2.4.3',
    'abbr' : '3.2.4.3',
    'name' : { 'en': 'Mysterious' },
    'description' : 'Use this domain for words that describe something that is difficult to understand.',
    'value' : '3.2.4.3 Mysterious'
  },
  {
    'guid' : '541dfa10-bf97-4713-a534-9cbcc7f66bc9',
    'id' : '3.2.5',
    'code' : '3.2.5',
    'abbr' : '3.2.5',
    'name' : { 'en': 'Opinion' },
    'description' : 'Use this domain for situations in which a question or issues are being debated, more than one option is possible, and a person chooses to think in one way about the question or issue.',
    'value' : '3.2.5 Opinion'
  },
  {
    'guid' : 'c0f4715e-55c9-4379-ab6f-ad561a5e7151',
    'id' : '3.2.5.1',
    'code' : '3.2.5.1',
    'abbr' : '3.2.5.1',
    'name' : { 'en': 'Believe' },
    'description' : 'Use this domain for words referring to believing that something is true.',
    'value' : '3.2.5.1 Believe'
  },
  {
    'guid' : '691bdba3-c216-4b42-877c-674bbdb517a7',
    'id' : '3.2.5.1.1',
    'code' : '3.2.5.1.1',
    'abbr' : '3.2.5.1.1',
    'name' : { 'en': 'Trust' },
    'description' : 'Use this domain for words referring to trusting someone--believing that someone is honest and will not do something bad to you.',
    'value' : '3.2.5.1.1 Trust'
  },
  {
    'guid' : '0f46cb61-7bb5-410d-abc5-4a75dc80a24f',
    'id' : '3.2.5.2',
    'code' : '3.2.5.2',
    'abbr' : '3.2.5.2',
    'name' : { 'en': 'Disbelief' },
    'description' : 'Use this domain for words referring to not believing something or someone.',
    'value' : '3.2.5.2 Disbelief'
  },
  {
    'guid' : '6fc8ae4d-9fad-462e-9ea0-c613c3c1cb5e',
    'id' : '3.2.5.3',
    'code' : '3.2.5.3',
    'abbr' : '3.2.5.3',
    'name' : { 'en': 'Doubt' },
    'description' : 'Use this domain for words related to doubt--not being sure if something is true or not.',
    'value' : '3.2.5.3 Doubt'
  },
  {
    'guid' : 'fd9b8618-1f62-419b-85c3-365a12e85523',
    'id' : '3.2.5.4',
    'code' : '3.2.5.4',
    'abbr' : '3.2.5.4',
    'name' : { 'en': 'Agree with someone' },
    'description' : 'Use this domain for when two people agree on something, think the same way about something, or agree on a decision.',
    'value' : '3.2.5.4 Agree with someone'
  },
  {
    'guid' : '1d5c798b-0f2d-49f2-bde6-cbcf2ef8fd02',
    'id' : '3.2.5.4.1',
    'code' : '3.2.5.4.1',
    'abbr' : '3.2.5.4.1',
    'name' : { 'en': 'Disagree' },
    'description' : 'Use this domain for when two people disagree about something.',
    'value' : '3.2.5.4.1 Disagree'
  },
  {
    'guid' : '2855cda6-a031-46aa-bf3f-718d94374d46',
    'id' : '3.2.5.4.2',
    'code' : '3.2.5.4.2',
    'abbr' : '3.2.5.4.2',
    'name' : { 'en': 'Protest' },
    'description' : 'Use this domain for words related to protesting--to say publicly that you do not like something.',
    'value' : '3.2.5.4.2 Protest'
  },
  {
    'guid' : 'b8e633f7-ca67-40cb-84e7-8b42887d161b',
    'id' : '3.2.5.5',
    'code' : '3.2.5.5',
    'abbr' : '3.2.5.5',
    'name' : { 'en': 'Philosophy' },
    'description' : 'Use this domain for words referring to a set of beliefs about truth.',
    'value' : '3.2.5.5 Philosophy'
  },
  {
    'guid' : '81e366fa-450b-42ad-b23f-7074dc7823e2',
    'id' : '3.2.5.6',
    'code' : '3.2.5.6',
    'abbr' : '3.2.5.6',
    'name' : { 'en': 'Attitude' },
    'description' : 'Use this domain for words referring to a person"s attitude--the way you think and feel about something.',
    'value' : '3.2.5.6 Attitude'
  },
  {
    'guid' : '3aab9c42-b696-4440-8e28-8380f5d25199',
    'id' : '3.2.5.7',
    'code' : '3.2.5.7',
    'abbr' : '3.2.5.7',
    'name' : { 'en': 'Extreme belief' },
    'description' : 'Use this domain for words referring to an extreme belief--something that you believe, that most people think is not good.',
    'value' : '3.2.5.7 Extreme belief'
  },
  {
    'guid' : '6c6259f0-eca6-4a30-8662-eedbaf293527',
    'id' : '3.2.5.8',
    'code' : '3.2.5.8',
    'abbr' : '3.2.5.8',
    'name' : { 'en': 'Change your mind' },
    'description' : 'Use this domain for words related to changing your mind--to change what you think about something, or change your plans or decisions.',
    'value' : '3.2.5.8 Change your mind'
  },
  {
    'guid' : 'a3f1d702-8ca1-457a-a569-eb6fdf696bbe',
    'id' : '3.2.5.9',
    'code' : '3.2.5.9',
    'abbr' : '3.2.5.9',
    'name' : { 'en': 'Approve of something' },
    'description' : 'Use this domain for words related to approving of doing something--to think that doing something is good.',
    'value' : '3.2.5.9 Approve of something'
  },
  {
    'guid' : '9f868391-f1df-4682-bdcb-2c2c799006cf',
    'id' : '3.2.6',
    'code' : '3.2.6',
    'abbr' : '3.2.6',
    'name' : { 'en': 'Remember' },
    'description' : 'Use this domain for words referring to remembering something you know.',
    'value' : '3.2.6 Remember'
  },
  {
    'guid' : 'ce30eb9c-8260-476b-878c-0a078d596955',
    'id' : '3.2.6.1',
    'code' : '3.2.6.1',
    'abbr' : '3.2.6.1',
    'name' : { 'en': 'Forget' },
    'description' : 'Use this domain for words referring to forgetting something.',
    'value' : '3.2.6.1 Forget'
  },
  {
    'guid' : 'a86b2e14-1299-4f59-842c-c4c5a401aace',
    'id' : '3.2.6.2',
    'code' : '3.2.6.2',
    'abbr' : '3.2.6.2',
    'name' : { 'en': 'Recognize' },
    'description' : 'Use this domain for words referring to recognizing something.',
    'value' : '3.2.6.2 Recognize'
  },
  {
    'guid' : '65a928b8-8587-48be-8f23-41f85549c547',
    'id' : '3.2.6.3',
    'code' : '3.2.6.3',
    'abbr' : '3.2.6.3',
    'name' : { 'en': 'Memorize' },
    'description' : 'Use this domain for words referring to memorizing something--to think hard about something so that you will not forget it.',
    'value' : '3.2.6.3 Memorize'
  },
  {
    'guid' : '45e90d41-a462-4671-968f-92166378b3f0',
    'id' : '3.2.6.4',
    'code' : '3.2.6.4',
    'abbr' : '3.2.6.4',
    'name' : { 'en': 'Remind' },
    'description' : 'Use this domain for words referring to reminding someone about something--to make someone remember something.',
    'value' : '3.2.6.4 Remind'
  },
  {
    'guid' : '5e3bb9e5-fd70-4c5a-95a0-938bc4876a47',
    'id' : '3.2.7',
    'code' : '3.2.7',
    'abbr' : '3.2.7',
    'name' : { 'en': 'Expect' },
    'description' : 'Use this domain for words referring to thinking about the future.',
    'value' : '3.2.7 Expect'
  },
  {
    'guid' : 'aaf63abd-c8a5-4546-8fc1-a51c6e607683',
    'id' : '3.2.7.1',
    'code' : '3.2.7.1',
    'abbr' : '3.2.7.1',
    'name' : { 'en': 'Hope' },
    'description' : 'Use this domain for words related to hoping that something will happen--to want something good to happen in the future.',
    'value' : '3.2.7.1 Hope'
  },
  {
    'guid' : '218c1d59-0ebb-4936-b9cf-0a93e88aa729',
    'id' : '3.2.7.2',
    'code' : '3.2.7.2',
    'abbr' : '3.2.7.2',
    'name' : { 'en': 'Hopeless' },
    'description' : 'Use this domain for words related to feeling hopeless--to thinking that nothing good will happen in the future.',
    'value' : '3.2.7.2 Hopeless'
  },
  {
    'guid' : 'c956a98a-4c85-4c85-868d-f27f44bd6422',
    'id' : '3.2.7.3',
    'code' : '3.2.7.3',
    'abbr' : '3.2.7.3',
    'name' : { 'en': 'Predict' },
    'description' : 'Use this domain for words referring to predicting the future--saying what you think will happen.',
    'value' : '3.2.7.3 Predict'
  },
  {
    'guid' : '05f95abb-163a-4927-83c5-8c81ef7b769c',
    'id' : '3.2.8',
    'code' : '3.2.8',
    'abbr' : '3.2.8',
    'name' : { 'en': 'Tendency' },
    'description' : 'Use this domain for words indicating that the speaker thinks that something tends to be a certain way.',
    'value' : '3.2.8 Tendency'
  },
  {
    'guid' : 'b50f39cb-3152-4d56-9ddc-4b98f763e76a',
    'id' : '3.3',
    'code' : '3.3',
    'abbr' : '3.3',
    'name' : { 'en': 'Want' },
    'description' : 'Use this domain for words related to wanting something or wanting to do something.',
    'value' : '3.3 Want'
  },
  {
    'guid' : 'af4ac058-d4b3-4c7a-ade8-6af762d0486d',
    'id' : '3.3.1',
    'code' : '3.3.1',
    'abbr' : '3.3.1',
    'name' : { 'en': 'Decide, plan' },
    'description' : 'Use this domain for words related to deciding to do something.',
    'value' : '3.3.1 Decide, plan'
  },
  {
    'guid' : 'b6b73d41-e23f-4f22-b01e-7e75f4115fce',
    'id' : '3.3.1.1',
    'code' : '3.3.1.1',
    'abbr' : '3.3.1.1',
    'name' : { 'en': 'Purpose, goal' },
    'description' : 'Use this domain for words referring to a goal--something that you want to do or something that you want to happen.',
    'value' : '3.3.1.1 Purpose, goal'
  },
  {
    'guid' : 'fed2b7bd-2315-4085-b0a7-2ced988120f3',
    'id' : '3.3.1.2',
    'code' : '3.3.1.2',
    'abbr' : '3.3.1.2',
    'name' : { 'en': 'Choose' },
    'description' : 'Use this domain for words related to choosing something--to want one thing (or person) from a group of things, or to choose to do one thing from several possible things you could do.',
    'value' : '3.3.1.2 Choose'
  },
  {
    'guid' : '7337b2dd-b574-4a41-affa-2dfad7f6cdbc',
    'id' : '3.3.1.3',
    'code' : '3.3.1.3',
    'abbr' : '3.3.1.3',
    'name' : { 'en': 'Cast lots' },
    'description' : 'Use this domain for words referring to casting lots--to make a decision by chance.',
    'value' : '3.3.1.3 Cast lots'
  },
  {
    'guid' : '7bf77556-64df-428e-bfdd-095af5bfeeda',
    'id' : '3.3.1.4',
    'code' : '3.3.1.4',
    'abbr' : '3.3.1.4',
    'name' : { 'en': 'Intend' },
    'description' : 'Use this domain for words related to intending to do something--to do something intentionally and not by accident.',
    'value' : '3.3.1.4 Intend'
  },
  {
    'guid' : '26bc089a-a989-4763-be6c-05d127d1c0e8',
    'id' : '3.3.1.5',
    'code' : '3.3.1.5',
    'abbr' : '3.3.1.5',
    'name' : { 'en': 'Deliberately' },
    'description' : 'Use this domain for words related to doing something deliberately--to intend to do something, as opposed to doing something accidentally.',
    'value' : '3.3.1.5 Deliberately'
  },
  {
    'guid' : '15fb022e-1b45-41e9-bd8a-09ddc9dc6acd',
    'id' : '3.3.1.6',
    'code' : '3.3.1.6',
    'abbr' : '3.3.1.6',
    'name' : { 'en': 'Determined' },
    'description' : 'Use this domain for words related to being determined to do something--deciding to do something and not letting anything stop you.',
    'value' : '3.3.1.6 Determined'
  },
  {
    'guid' : 'cc121082-2d07-484e-8a6f-7382f7d71f39',
    'id' : '3.3.1.7',
    'code' : '3.3.1.7',
    'abbr' : '3.3.1.7',
    'name' : { 'en': 'Stubborn' },
    'description' : 'Use this domain for words related to being stubborn--to be unwilling to change a decision; to be unwilling to do something someone wants you to do; or to do what you want to do, even though other people do not want you to do it.',
    'value' : '3.3.1.7 Stubborn'
  },
  {
    'guid' : '41b80f5d-0298-4d3c-b1a3-6d5e6c3985b1',
    'id' : '3.3.1.8',
    'code' : '3.3.1.8',
    'abbr' : '3.3.1.8',
    'name' : { 'en': 'Lust' },
    'description' : 'Use this domain for words related to lusting for something--to want something bad or forbidden.',
    'value' : '3.3.1.8 Lust'
  },
  {
    'guid' : '45f7b003-ade3-4efc-8dee-259dcbf80a4a',
    'id' : '3.3.2',
    'code' : '3.3.2',
    'abbr' : '3.3.2',
    'name' : { 'en': 'Request' },
    'description' : 'Use this domain for words related to requesting something--to ask for something, or ask someone to do something.',
    'value' : '3.3.2 Request'
  },
  {
    'guid' : 'd2e73238-ff99-4ba3-8ce6-d8ae98721710',
    'id' : '3.3.2.1',
    'code' : '3.3.2.1',
    'abbr' : '3.3.2.1',
    'name' : { 'en': 'Agree to do something' },
    'description' : 'Use this domain for words related to agreeing to do something.',
    'value' : '3.3.2.1 Agree to do something'
  },
  {
    'guid' : '1137590c-6f2f-4b69-b04e-f6a890a335a2',
    'id' : '3.3.2.2',
    'code' : '3.3.2.2',
    'abbr' : '3.3.2.2',
    'name' : { 'en': 'Refuse to do something' },
    'description' : 'Use this domain for words related to refusing to do something.',
    'value' : '3.3.2.2 Refuse to do something'
  },
  {
    'guid' : '593073d6-9893-4670-98fb-c485406a950b',
    'id' : '3.3.2.3',
    'code' : '3.3.2.3',
    'abbr' : '3.3.2.3',
    'name' : { 'en': 'Intercede' },
    'description' : 'Use this domain for words related to interceding for someone--to say something to someone because you want them to do something good for someone else.',
    'value' : '3.3.2.3 Intercede'
  },
  {
    'guid' : '6b19e828-f597-4d0d-b7a6-f52f3bbd041f',
    'id' : '3.3.2.4',
    'code' : '3.3.2.4',
    'abbr' : '3.3.2.4',
    'name' : { 'en': 'Willing' },
    'description' : 'Use this domain for words related to being willing to do something.',
    'value' : '3.3.2.4 Willing'
  },
  {
    'guid' : '5ea9301a-9906-4e81-97cf-48ee95a54c63',
    'id' : '3.3.3',
    'code' : '3.3.3',
    'abbr' : '3.3.3',
    'name' : { 'en': 'Influence' },
    'description' : 'Use this domain for words related to influencing someone--to do something because you want someone to change his thinking.',
    'value' : '3.3.3 Influence'
  },
  {
    'guid' : '8ea4e0d4-71a2-4583-a1fe-f1a941af8478',
    'id' : '3.3.3.1',
    'code' : '3.3.3.1',
    'abbr' : '3.3.3.1',
    'name' : { 'en': 'Suggest' },
    'description' : 'Use this domain for words referring to suggesting something--saying that something might be good.',
    'value' : '3.3.3.1 Suggest'
  },
  {
    'guid' : '6bfb7813-3a7d-47e2-88e1-d54034c07e5d',
    'id' : '3.3.3.2',
    'code' : '3.3.3.2',
    'abbr' : '3.3.3.2',
    'name' : { 'en': 'Advise' },
    'description' : 'Use this domain for words referring to giving someone advice or counsel, for instance recommending a wise course of action.',
    'value' : '3.3.3.2 Advise'
  },
  {
    'guid' : '9d428e57-e125-4575-b165-9bc6fd4ec507',
    'id' : '3.3.3.3',
    'code' : '3.3.3.3',
    'abbr' : '3.3.3.3',
    'name' : { 'en': 'Persuade' },
    'description' : 'Use this domain for words related to persuading someone--to try to get someone to do something or to change his thinking.',
    'value' : '3.3.3.3 Persuade'
  },
  {
    'guid' : 'ad56dc48-9c39-43f6-9386-f7df80d93cd4',
    'id' : '3.3.3.4',
    'code' : '3.3.3.4',
    'abbr' : '3.3.3.4',
    'name' : { 'en': 'Insist' },
    'description' : 'Use this domain for words related to insisting--to say strongly or repeatedly that someone must do something, because the other person does not want to do it.',
    'value' : '3.3.3.4 Insist'
  },
  {
    'guid' : '6fbe39fb-a4cb-4fcf-830b-8875fd4324e0',
    'id' : '3.3.3.5',
    'code' : '3.3.3.5',
    'abbr' : '3.3.3.5',
    'name' : { 'en': 'Compel' },
    'description' : 'Use this domain for words related to compelling someone to do something--to cause or force someone to do something that they do not want to do.',
    'value' : '3.3.3.5 Compel'
  },
  {
    'guid' : 'e6ec43ef-0100-4cf4-a047-c575ee8613b4',
    'id' : '3.3.3.6',
    'code' : '3.3.3.6',
    'abbr' : '3.3.3.6',
    'name' : { 'en': 'Control' },
    'description' : 'Use this domain for words related to controlling someone--to force someone to do what you want them to do by ordering them to do it, or by doing something so that they have no choice. Also use this domain for words related to controlling something, for instance to control a machine, so that it does what you want it to do.',
    'value' : '3.3.3.6 Control'
  },
  {
    'guid' : '3445e61b-61a3-4ede-93f5-402ebe9ca51c',
    'id' : '3.3.3.7',
    'code' : '3.3.3.7',
    'abbr' : '3.3.3.7',
    'name' : { 'en': 'Warn' },
    'description' : 'Use this domain for words related to warning someone--saying something to someone so that he will not do something bad.',
    'value' : '3.3.3.7 Warn'
  },
  {
    'guid' : 'c1c1dcb4-8fe5-43ac-9b91-b2b2bc33de5b',
    'id' : '3.3.3.8',
    'code' : '3.3.3.8',
    'abbr' : '3.3.3.8',
    'name' : { 'en': 'Threaten' },
    'description' : 'Use this domain for words related to threatening someone--to say that you will do something bad to someone if they don"t do what you want them to do.',
    'value' : '3.3.3.8 Threaten'
  },
  {
    'guid' : '8383b4cb-e14a-4d04-a8c3-9c5276384953',
    'id' : '3.3.4',
    'code' : '3.3.4',
    'abbr' : '3.3.4',
    'name' : { 'en': 'Ask permission' },
    'description' : 'Use this domain for words related to asking permission to do something. This domain is part of a scenario: You have authority over me. I want to do something. I ask you for permission to do it. You give permission or refuse permission. I obey you or disobey you.',
    'value' : '3.3.4 Ask permission'
  },
  {
    'guid' : 'a3e905b8-107b-4311-bb50-0abe151131b3',
    'id' : '3.3.4.1',
    'code' : '3.3.4.1',
    'abbr' : '3.3.4.1',
    'name' : { 'en': 'Give permission' },
    'description' : 'Use this domain for words related to giving someone permission to do something. This domain is about a scenario: You have authority over me. I want to do something. I ask you for permission to do it. You give permission or refuse permission.',
    'value' : '3.3.4.1 Give permission'
  },
  {
    'guid' : '8f46496a-d5b2-411d-944d-9d4d4b6f2e31',
    'id' : '3.3.4.2',
    'code' : '3.3.4.2',
    'abbr' : '3.3.4.2',
    'name' : { 'en': 'Refuse permission' },
    'description' : 'Use this domain for words related to refusing to permit someone to do something.',
    'value' : '3.3.4.2 Refuse permission'
  },
  {
    'guid' : 'b6baa8bf-7691-431d-8715-3937372b9da0',
    'id' : '3.3.4.3',
    'code' : '3.3.4.3',
    'abbr' : '3.3.4.3',
    'name' : { 'en': 'Exempt' },
    'description' : 'Use this domain for words related to being exempt from a law or obligation.',
    'value' : '3.3.4.3 Exempt'
  },
  {
    'guid' : '66fc9a08-3661-4dd5-94b0-1eea41fb4554',
    'id' : '3.3.4.4',
    'code' : '3.3.4.4',
    'abbr' : '3.3.4.4',
    'name' : { 'en': 'Prevent' },
    'description' : 'Use this domain for words related to preventing someone from doing something.',
    'value' : '3.3.4.4 Prevent'
  },
  {
    'guid' : 'c8c74ec6-3f7a-4b45-b0e9-399b97c4a800',
    'id' : '3.3.4.5',
    'code' : '3.3.4.5',
    'abbr' : '3.3.4.5',
    'name' : { 'en': 'Free to do what you want' },
    'description' : 'Use this domain for words related to freedom--when you can do the things that you want to do.',
    'value' : '3.3.4.5 Free to do what you want'
  },
  {
    'guid' : '7c9c6263-9f7d-472c-a4c9-1767015d41fe',
    'id' : '3.3.5',
    'code' : '3.3.5',
    'abbr' : '3.3.5',
    'name' : { 'en': 'Offer' },
    'description' : 'Use this domain for words related to offering to do something for someone.',
    'value' : '3.3.5 Offer'
  },
  {
    'guid' : 'dafc4b97-2b70-4986-b2b4-c05eb060786d',
    'id' : '3.3.5.1',
    'code' : '3.3.5.1',
    'abbr' : '3.3.5.1',
    'name' : { 'en': 'Accept' },
    'description' : 'Use this domain for words referring to accepting something such as an offer, invitation, or request.',
    'value' : '3.3.5.1 Accept'
  },
  {
    'guid' : '612424b8-997e-4661-a452-772e14a3c4a0',
    'id' : '3.3.5.2',
    'code' : '3.3.5.2',
    'abbr' : '3.3.5.2',
    'name' : { 'en': 'Reject' },
    'description' : 'Use this domain for words referring to rejecting something such as an offer, invitation, or request.',
    'value' : '3.3.5.2 Reject'
  },
  {
    'guid' : 'cb95189c-8c74-465b-af07-48e08dbf7c39',
    'id' : '3.4',
    'code' : '3.4',
    'abbr' : '3.4',
    'name' : { 'en': 'Emotion' },
    'description' : 'Use this domain for general words related to feelings and emotions.',
    'value' : '3.4 Emotion'
  },
  {
    'guid' : '474aa982-8350-47e2-a983-e1e2bce9d928',
    'id' : '3.4.1',
    'code' : '3.4.1',
    'abbr' : '3.4.1',
    'name' : { 'en': 'Feel good' },
    'description' : 'Use this domain for general words related to positive emotions.',
    'value' : '3.4.1 Feel good'
  },
  {
    'guid' : 'd030f0c7-31a3-47da-be35-46f1eba63ae9',
    'id' : '3.4.1.1',
    'code' : '3.4.1.1',
    'abbr' : '3.4.1.1',
    'name' : { 'en': 'Like, love' },
    'description' : 'Use this domain for words related to liking something or someone, or liking to do something.',
    'value' : '3.4.1.1 Like, love'
  },
  {
    'guid' : '50eb32a2-6dbb-4b7c-b370-aacdcfeaf5fc',
    'id' : '3.4.1.1.1',
    'code' : '3.4.1.1.1',
    'abbr' : '3.4.1.1.1',
    'name' : { 'en': 'Enjoy doing something' },
    'description' : 'Use this domain for words related to enjoying doing something.',
    'value' : '3.4.1.1.1 Enjoy doing something'
  },
  {
    'guid' : 'ce6d5e60-7cf6-46ab-bd02-453ec7b04f7a',
    'id' : '3.4.1.1.2',
    'code' : '3.4.1.1.2',
    'abbr' : '3.4.1.1.2',
    'name' : { 'en': 'Self-esteem' },
    'description' : 'Use this domain for words related to feeling good about yourself.',
    'value' : '3.4.1.1.2 Self-esteem'
  },
  {
    'guid' : 'ef409fc6-bd89-4cc6-ade5-abb882272313',
    'id' : '3.4.1.1.3',
    'code' : '3.4.1.1.3',
    'abbr' : '3.4.1.1.3',
    'name' : { 'en': 'Prefer' },
    'description' : 'Use this domain for words related to preferring one thing over another--to like something more than something else.',
    'value' : '3.4.1.1.3 Prefer'
  },
  {
    'guid' : 'ac550d1f-ec74-46a8-bf81-7832ace533ee',
    'id' : '3.4.1.1.4',
    'code' : '3.4.1.1.4',
    'abbr' : '3.4.1.1.4',
    'name' : { 'en': 'Popular' },
    'description' : 'Use this domain for words describing something that many people like.',
    'value' : '3.4.1.1.4 Popular'
  },
  {
    'guid' : 'e54cd744-d106-4bcf-bd07-b8783c075c21',
    'id' : '3.4.1.1.5',
    'code' : '3.4.1.1.5',
    'abbr' : '3.4.1.1.5',
    'name' : { 'en': 'Fashionable' },
    'description' : 'Use this domain for words describing something that many people like.',
    'value' : '3.4.1.1.5 Fashionable'
  },
  {
    'guid' : 'f0fdbdfa-094e-4bec-ae19-af23d2c02ed6',
    'id' : '3.4.1.1.6',
    'code' : '3.4.1.1.6',
    'abbr' : '3.4.1.1.6',
    'name' : { 'en': 'Contentment' },
    'description' : 'Use this domain for words related to feeling contented.',
    'value' : '3.4.1.1.6 Contentment'
  },
  {
    'guid' : '4aedd6d3-8f4b-4986-8d51-b0ace0137bf0',
    'id' : '3.4.1.1.7',
    'code' : '3.4.1.1.7',
    'abbr' : '3.4.1.1.7',
    'name' : { 'en': 'Happy for' },
    'description' : 'Use this domain for words related to feeling happy for someone--to feel good because something good happened to someone. The opposite is jealousy and envy.',
    'value' : '3.4.1.1.7 Happy for'
  },
  {
    'guid' : '46b13a77-fe12-49fb-afbe-826480ec97f4',
    'id' : '3.4.1.1.8',
    'code' : '3.4.1.1.8',
    'abbr' : '3.4.1.1.8',
    'name' : { 'en': 'Pleased with' },
    'description' : 'Use this domain for words related to feeling pleased with someone--to feel good because someone did something good.',
    'value' : '3.4.1.1.8 Pleased with'
  },
  {
    'guid' : 'afa77a2a-8b0f-4a39-91bc-040e90ffbb3a',
    'id' : '3.4.1.2',
    'code' : '3.4.1.2',
    'abbr' : '3.4.1.2',
    'name' : { 'en': 'Happy' },
    'description' : 'Use this domain for words related to feeling happy--to feel good when something good happens (such as receiving a gift, hearing good news, or watching something good happening).',
    'value' : '3.4.1.2 Happy'
  },
  {
    'guid' : '26fb2e94-b8fe-4216-9057-ca17a71df83b',
    'id' : '3.4.1.2.1',
    'code' : '3.4.1.2.1',
    'abbr' : '3.4.1.2.1',
    'name' : { 'en': 'Relaxed' },
    'description' : 'Use this domain for words related to feeling relaxed--to feel good when you are not working and nothing bad is happening.',
    'value' : '3.4.1.2.1 Relaxed'
  },
  {
    'guid' : '9351d5b6-5a87-422a-9e82-ca6a0bacd3e9',
    'id' : '3.4.1.2.2',
    'code' : '3.4.1.2.2',
    'abbr' : '3.4.1.2.2',
    'name' : { 'en': 'Calm' },
    'description' : 'Use this domain for words related to feeling calm.',
    'value' : '3.4.1.2.2 Calm'
  },
  {
    'guid' : '0e590da7-c027-42e0-b580-f65686cee461',
    'id' : '3.4.1.3',
    'code' : '3.4.1.3',
    'abbr' : '3.4.1.3',
    'name' : { 'en': 'Surprise' },
    'description' : 'Use this domain for words related to feeling surprised--to feel something when something unexpected, unusual, or amazing happens.',
    'value' : '3.4.1.3 Surprise'
  },
  {
    'guid' : 'f9d020d6-b129-4bb8-9509-3b4a6c27482e',
    'id' : '3.4.1.4',
    'code' : '3.4.1.4',
    'abbr' : '3.4.1.4',
    'name' : { 'en': 'Interested' },
    'description' : 'Use this domain for words related to feeling interested.',
    'value' : '3.4.1.4 Interested'
  },
  {
    'guid' : '00364f0c-9a3a-4910-a82e-1ffbc4d4137f',
    'id' : '3.4.1.4.1',
    'code' : '3.4.1.4.1',
    'abbr' : '3.4.1.4.1',
    'name' : { 'en': 'Excited' },
    'description' : 'Use this domain for words related to feeling excited--to feel good when something good has happening or is about to happen.',
    'value' : '3.4.1.4.1 Excited'
  },
  {
    'guid' : '85912845-21b0-41eb-8b8c-1f5c3d53df08',
    'id' : '3.4.1.4.2',
    'code' : '3.4.1.4.2',
    'abbr' : '3.4.1.4.2',
    'name' : { 'en': 'Enthusiastic' },
    'description' : 'Use this domain for words related to feeling enthusiastic--to feel very good because you want to do something or you want something to happen.',
    'value' : '3.4.1.4.2 Enthusiastic'
  },
  {
    'guid' : '1c0c4951-03b6-49b8-8a8e-724397cfd5a7',
    'id' : '3.4.1.4.3',
    'code' : '3.4.1.4.3',
    'abbr' : '3.4.1.4.3',
    'name' : { 'en': 'Obsessed' },
    'description' : 'Use this domain for words related to feeling obsessed--to be very interested in something for a long time.',
    'value' : '3.4.1.4.3 Obsessed'
  },
  {
    'guid' : 'bb2a112f-af6f-4a54-bbf0-ba7b8289e58b',
    'id' : '3.4.1.4.4',
    'code' : '3.4.1.4.4',
    'abbr' : '3.4.1.4.4',
    'name' : { 'en': 'Attract' },
    'description' : 'Use this domain for words related to attracting someone"s attention to something. In a typical situation, a person sees something with an interesting quality. The person moves closer, pays close attention to the thing, and possibly does something to it.',
    'value' : '3.4.1.4.4 Attract'
  },
  {
    'guid' : '5c31bdf6-901f-4f14-ab64-7a99524710fc',
    'id' : '3.4.1.4.5',
    'code' : '3.4.1.4.5',
    'abbr' : '3.4.1.4.5',
    'name' : { 'en': 'Indifferent' },
    'description' : 'Use this domain for words related to feeling indifferent about something.',
    'value' : '3.4.1.4.5 Indifferent'
  },
  {
    'guid' : '0fa0be21-2246-40b2-86b1-ca572fe8c16c',
    'id' : '3.4.1.4.6',
    'code' : '3.4.1.4.6',
    'abbr' : '3.4.1.4.6',
    'name' : { 'en': 'Uninterested, bored' },
    'description' : 'Use this domain for words related to feeling uninterested or bored--when someone is not interested in something.',
    'value' : '3.4.1.4.6 Uninterested, bored'
  },
  {
    'guid' : 'e0a8e1d9-c43e-4092-a8dc-476a3417924e',
    'id' : '3.4.1.5',
    'code' : '3.4.1.5',
    'abbr' : '3.4.1.5',
    'name' : { 'en': 'Confident' },
    'description' : 'Use this domain for words related to feeling confident--to feel sure that you can do something.',
    'value' : '3.4.1.5 Confident'
  },
  {
    'guid' : '60dc7cf6-0a41-4cd3-99a6-0b7a71488e7e',
    'id' : '3.4.2',
    'code' : '3.4.2',
    'abbr' : '3.4.2',
    'name' : { 'en': 'Feel bad' },
    'description' : 'Use this domain for general words related to feeling bad.',
    'value' : '3.4.2 Feel bad'
  },
  {
    'guid' : '6142c173-161a-47d5-bdec-c827518fd67c',
    'id' : '3.4.2.1',
    'code' : '3.4.2.1',
    'abbr' : '3.4.2.1',
    'name' : { 'en': 'Sad' },
    'description' : 'Use this domain for words related to feeling sad--to feel bad because something bad has happened (such as losing something, hearing bad news, or watching something bad happening).',
    'value' : '3.4.2.1 Sad'
  },
  {
    'guid' : '82ecb5b3-9128-4b38-b9c5-612857417ceb',
    'id' : '3.4.2.1.1',
    'code' : '3.4.2.1.1',
    'abbr' : '3.4.2.1.1',
    'name' : { 'en': 'Dislike' },
    'description' : 'Use this domain for words related to not liking someone or something.',
    'value' : '3.4.2.1.1 Dislike'
  },
  {
    'guid' : '97393c87-07e2-4633-88f9-c8bf4d9b935c',
    'id' : '3.4.2.1.2',
    'code' : '3.4.2.1.2',
    'abbr' : '3.4.2.1.2',
    'name' : { 'en': 'Hate, detest' },
    'description' : 'Use this domain for words related to hating someone or something--to dislike someone or something very much.',
    'value' : '3.4.2.1.2 Hate, detest'
  },
  {
    'guid' : '8d5c28b8-91be-40b0-b6c2-4d5adbb495a3',
    'id' : '3.4.2.1.3',
    'code' : '3.4.2.1.3',
    'abbr' : '3.4.2.1.3',
    'name' : { 'en': 'Disgusted' },
    'description' : 'Use this domain for words related to feeling disgusted--to dislike something so much that you feel sick.',
    'value' : '3.4.2.1.3 Disgusted'
  },
  {
    'guid' : '2e30f02d-d1e6-489c-a1fb-8b9fb6ecd819',
    'id' : '3.4.2.1.4',
    'code' : '3.4.2.1.4',
    'abbr' : '3.4.2.1.4',
    'name' : { 'en': 'Disappointed' },
    'description' : 'Use this domain for words related to feeling disappointed--to feel bad because something did not happen that you wanted to happen or someone did not do something that you wanted them to do.',
    'value' : '3.4.2.1.4 Disappointed'
  },
  {
    'guid' : '39611e8d-cc67-4c84-977c-094c5cbe9dbc',
    'id' : '3.4.2.1.5',
    'code' : '3.4.2.1.5',
    'abbr' : '3.4.2.1.5',
    'name' : { 'en': 'Lonely' },
    'description' : 'Use this domain for words related to feeling lonely--to feel bad because you are alone and not with people you love.',
    'value' : '3.4.2.1.5 Lonely'
  },
  {
    'guid' : 'bfe8902a-32a7-4092-93b2-9dcf3dce205f',
    'id' : '3.4.2.1.6',
    'code' : '3.4.2.1.6',
    'abbr' : '3.4.2.1.6',
    'name' : { 'en': 'Upset' },
    'description' : 'Use this domain for word related to feeling upset--to feel very bad because someone has done something bad to you or because something bad has happened to you, so that your thinking and behavior is affected.',
    'value' : '3.4.2.1.6 Upset'
  },
  {
    'guid' : '67d10a00-fda8-4a3a-becd-f3ae3b00fcca',
    'id' : '3.4.2.1.7',
    'code' : '3.4.2.1.7',
    'abbr' : '3.4.2.1.7',
    'name' : { 'en': 'Shock' },
    'description' : 'Use this domain for words related to feeling shocked--feeling both surprised and angry when something very bad suddenly happens or when someone does something very bad.',
    'value' : '3.4.2.1.7 Shock'
  },
  {
    'guid' : '262fc4ae-7735-465b-934b-2125d95de147',
    'id' : '3.4.2.1.8',
    'code' : '3.4.2.1.8',
    'abbr' : '3.4.2.1.8',
    'name' : { 'en': 'Jealous' },
    'description' : 'Use this domain for words related to feeling jealous--to feel bad when someone does well, has something good, receives something good, or something good happens to them, because you want what they have. Also use this domain for words for when a husband (or wife) is jealous because he thinks his wife loves someone else.',
    'value' : '3.4.2.1.8 Jealous'
  },
  {
    'guid' : 'f3654a7f-d16e-4870-9ef0-4b4268faeffb',
    'id' : '3.4.2.1.9',
    'code' : '3.4.2.1.9',
    'abbr' : '3.4.2.1.9',
    'name' : { 'en': 'Discontent' },
    'description' : 'Use this domain for words related to feeling discontent.',
    'value' : '3.4.2.1.9 Discontent'
  },
  {
    'guid' : 'da39c0d9-a5c1-4f10-bd3b-4e988abcab5a',
    'id' : '3.4.2.2',
    'code' : '3.4.2.2',
    'abbr' : '3.4.2.2',
    'name' : { 'en': 'Sorry' },
    'description' : 'Use this domain for words related to feeling sorry--to feel bad about something bad that you did.',
    'value' : '3.4.2.2 Sorry'
  },
  {
    'guid' : '43cb3488-711b-4d9d-9d0e-03d0c1f6eb8b',
    'id' : '3.4.2.2.1',
    'code' : '3.4.2.2.1',
    'abbr' : '3.4.2.2.1',
    'name' : { 'en': 'Ashamed' },
    'description' : 'Use this domain for words related to feeling ashamed--to feel bad because people think you did something bad.',
    'value' : '3.4.2.2.1 Ashamed'
  },
  {
    'guid' : 'e08e252a-9227-42e4-bcb8-b803d25071b6',
    'id' : '3.4.2.2.2',
    'code' : '3.4.2.2.2',
    'abbr' : '3.4.2.2.2',
    'name' : { 'en': 'Embarrassed' },
    'description' : 'Use this domain for words related to feeling embarrassed--to feel bad in front of other people because you did or said something that makes you seem stupid.',
    'value' : '3.4.2.2.2 Embarrassed'
  },
  {
    'guid' : '2a62f8e4-7da3-4f37-bf44-e24033c99c00',
    'id' : '3.4.2.3',
    'code' : '3.4.2.3',
    'abbr' : '3.4.2.3',
    'name' : { 'en': 'Angry' },
    'description' : 'Use this domain for words related to feeling angry--to feel bad when someone does something wrong and to want to do something bad to them.',
    'value' : '3.4.2.3 Angry'
  },
  {
    'guid' : '8ce6709f-f772-4638-a1aa-c132666f3563',
    'id' : '3.4.2.3.1',
    'code' : '3.4.2.3.1',
    'abbr' : '3.4.2.3.1',
    'name' : { 'en': 'Annoyed' },
    'description' : 'Use this domain for words related to feeling annoyed--to feel a little angry because someone keeps doing something you don"t like.',
    'value' : '3.4.2.3.1 Annoyed'
  },
  {
    'guid' : '2b6f9af7-04ee-4030-a2cd-87d55959caa8',
    'id' : '3.4.2.4',
    'code' : '3.4.2.4',
    'abbr' : '3.4.2.4',
    'name' : { 'en': 'Afraid' },
    'description' : 'Use this domain for words related to fear--to feel bad because you think something bad might happen to you.',
    'value' : '3.4.2.4 Afraid'
  },
  {
    'guid' : 'ba1e8d2b-7d3e-4b65-a9be-ee1a4063c796',
    'id' : '3.4.2.4.1',
    'code' : '3.4.2.4.1',
    'abbr' : '3.4.2.4.1',
    'name' : { 'en': 'Worried' },
    'description' : 'Use this domain for words related to feeling worried--to feel bad because you think something bad might happen.',
    'value' : '3.4.2.4.1 Worried'
  },
  {
    'guid' : '64e3d6b1-1d61-454c-97ab-e1d7cb0a8917',
    'id' : '3.4.2.4.2',
    'code' : '3.4.2.4.2',
    'abbr' : '3.4.2.4.2',
    'name' : { 'en': 'Nervous' },
    'description' : 'Use this domain for words related to feeling nervous--to be worried and frightened that something bad may happen, so that you are unable to relax.',
    'value' : '3.4.2.4.2 Nervous'
  },
  {
    'guid' : '100e62a6-b6f4-4b30-b317-0517d6b102a9',
    'id' : '3.4.2.4.3',
    'code' : '3.4.2.4.3',
    'abbr' : '3.4.2.4.3',
    'name' : { 'en': 'Shy, timid' },
    'description' : 'Use this domain for words related to feeling shy--to feel bad (afraid) when you are with people because you think they might think something bad about you if you say or do something (for instance, being afraid to talk, feeling inadequate to do what is required in a social situation, or not feeling as good as other people).',
    'value' : '3.4.2.4.3 Shy, timid'
  },
  {
    'guid' : '8052744f-7a9a-49da-89e3-4c518126180b',
    'id' : '3.4.2.5',
    'code' : '3.4.2.5',
    'abbr' : '3.4.2.5',
    'name' : { 'en': 'Confused' },
    'description' : 'Use this domain for words related to feeling confused--to be worried and uncertain about what something means or what to do.',
    'value' : '3.4.2.5 Confused'
  },
  {
    'guid' : 'c9aee4df-ac3e-4159-bd1a-060db1a5f070',
    'id' : '3.5',
    'code' : '3.5',
    'abbr' : '3.5',
    'name' : { 'en': 'Communication' },
    'description' : 'Use this domain for general words referring to communication of all kinds.',
    'value' : '3.5 Communication'
  },
  {
    'guid' : '9efa7949-de15-499a-b382-4560e06c4fb4',
    'id' : '3.5.1',
    'code' : '3.5.1',
    'abbr' : '3.5.1',
    'name' : { 'en': 'Say' },
    'description' : 'Use this domain for words related to saying something.',
    'value' : '3.5.1 Say'
  },
  {
    'guid' : '1e9a0881-f715-4057-9af8-251cb8eec9da',
    'id' : '3.5.1.1',
    'code' : '3.5.1.1',
    'abbr' : '3.5.1.1',
    'name' : { 'en': 'Voice' },
    'description' : 'Use this domain for words referring to a person"s voice and the way it sounds--the kind of sound a person makes when they speak or sing.',
    'value' : '3.5.1.1 Voice'
  },
  {
    'guid' : '8c7da1d1-d7d7-470c-b6a3-edefb0e9a4d2',
    'id' : '3.5.1.1.1',
    'code' : '3.5.1.1.1',
    'abbr' : '3.5.1.1.1',
    'name' : { 'en': 'Shout' },
    'description' : 'Use this domain for words related to shouting--to speak loudly.',
    'value' : '3.5.1.1.1 Shout'
  },
  {
    'guid' : '9612bdd6-15cb-4269-aaf9-481c7b35b5dd',
    'id' : '3.5.1.1.2',
    'code' : '3.5.1.1.2',
    'abbr' : '3.5.1.1.2',
    'name' : { 'en': 'Speak quietly' },
    'description' : 'Use this domain for words that describe a person speaking quietly.',
    'value' : '3.5.1.1.2 Speak quietly'
  },
  {
    'guid' : 'b2fa4109-1165-4c1f-9613-c5b2d349d2d4',
    'id' : '3.5.1.1.3',
    'code' : '3.5.1.1.3',
    'abbr' : '3.5.1.1.3',
    'name' : { 'en': 'Speak a lot' },
    'description' : 'Use this domain for words related to speaking a lot.',
    'value' : '3.5.1.1.3 Speak a lot'
  },
  {
    'guid' : '78b0ad1b-0766-41ba-b788-d176addd5e9f',
    'id' : '3.5.1.1.4',
    'code' : '3.5.1.1.4',
    'abbr' : '3.5.1.1.4',
    'name' : { 'en': 'Speak little' },
    'description' : 'Use this domain for words related to speaking a little, either because you do not like to talk, or you think you should not talk.',
    'value' : '3.5.1.1.4 Speak little'
  },
  {
    'guid' : 'f5642647-9b9c-499b-a66e-349593c863f1',
    'id' : '3.5.1.1.5',
    'code' : '3.5.1.1.5',
    'abbr' : '3.5.1.1.5',
    'name' : { 'en': 'Say nothing' },
    'description' : 'Use this domain for words related to being silent--to say nothing for some time.',
    'value' : '3.5.1.1.5 Say nothing'
  },
  {
    'guid' : 'e482bb5a-5a32-4bc5-a0de-32cbe0aa7908',
    'id' : '3.5.1.1.6',
    'code' : '3.5.1.1.6',
    'abbr' : '3.5.1.1.6',
    'name' : { 'en': 'Speech style' },
    'description' : 'Use this domain for words describing the way a person talks in a particular social situation.',
    'value' : '3.5.1.1.6 Speech style'
  },
  {
    'guid' : 'e96f6860-0914-4324-9c49-48f24a0ff7f1',
    'id' : '3.5.1.1.7',
    'code' : '3.5.1.1.7',
    'abbr' : '3.5.1.1.7',
    'name' : { 'en': 'Speak well' },
    'description' : 'Use this domain for words related to speaking well.',
    'value' : '3.5.1.1.7 Speak well'
  },
  {
    'guid' : '8f779877-7d86-4683-8a8b-298c7fc62815',
    'id' : '3.5.1.1.8',
    'code' : '3.5.1.1.8',
    'abbr' : '3.5.1.1.8',
    'name' : { 'en': 'Speak poorly' },
    'description' : 'Use this domain for words related to speaking poorly.',
    'value' : '3.5.1.1.8 Speak poorly'
  },
  {
    'guid' : 'e45b8bac-9623-4f84-a113-9dec13a8db64',
    'id' : '3.5.1.2',
    'code' : '3.5.1.2',
    'abbr' : '3.5.1.2',
    'name' : { 'en': 'Talk about a subject' },
    'description' : 'Use this domain for words related to talking about a subject.',
    'value' : '3.5.1.2 Talk about a subject'
  },
  {
    'guid' : '1148684a-0f44-4b5a-9e3e-3823163cd4a1',
    'id' : '3.5.1.2.1',
    'code' : '3.5.1.2.1',
    'abbr' : '3.5.1.2.1',
    'name' : { 'en': 'Announce' },
    'description' : 'Use this domain for words related to announcing something--communicating something to many people.',
    'value' : '3.5.1.2.1 Announce'
  },
  {
    'guid' : 'dc1ab28c-3e1e-474c-8359-2548b7ad5595',
    'id' : '3.5.1.2.2',
    'code' : '3.5.1.2.2',
    'abbr' : '3.5.1.2.2',
    'name' : { 'en': 'Describe' },
    'description' : 'Use this domain for words related to describing something--to say some things about something.',
    'value' : '3.5.1.2.2 Describe'
  },
  {
    'guid' : 'e080687b-0900-4dd0-9677-e3aaa3eae641',
    'id' : '3.5.1.2.3',
    'code' : '3.5.1.2.3',
    'abbr' : '3.5.1.2.3',
    'name' : { 'en': 'Explain' },
    'description' : 'Use this domain for words related to explaining something--to help someone to understand something.',
    'value' : '3.5.1.2.3 Explain'
  },
  {
    'guid' : '7ad54364-ca8c-4b7c-9748-f83efb44d0ea',
    'id' : '3.5.1.2.4',
    'code' : '3.5.1.2.4',
    'abbr' : '3.5.1.2.4',
    'name' : { 'en': 'Mention' },
    'description' : 'Use this domain for words related to mentioning something--to talk about something but without saying much about it.',
    'value' : '3.5.1.2.4 Mention'
  },
  {
    'guid' : '67d282f8-151d-429b-8183-6a7d2f5ac98d',
    'id' : '3.5.1.2.5',
    'code' : '3.5.1.2.5',
    'abbr' : '3.5.1.2.5',
    'name' : { 'en': 'Introduce' },
    'description' : 'Use this domain for words related to introducing a new subject--to start talking or writing about something new for the first time.',
    'value' : '3.5.1.2.5 Introduce'
  },
  {
    'guid' : '0539de86-f407-4b3d-b1b8-028822fb9f26',
    'id' : '3.5.1.2.6',
    'code' : '3.5.1.2.6',
    'abbr' : '3.5.1.2.6',
    'name' : { 'en': 'Repeat' },
    'description' : 'Use this domain for words related to repeating something--saying something a second time.',
    'value' : '3.5.1.2.6 Repeat'
  },
  {
    'guid' : '728bbc7c-e5b3-47d8-8532-72239e5c88bb',
    'id' : '3.5.1.2.7',
    'code' : '3.5.1.2.7',
    'abbr' : '3.5.1.2.7',
    'name' : { 'en': 'Summarize' },
    'description' : 'Use this domain for words referring to summarizing what you have said or what someone else has said.',
    'value' : '3.5.1.2.7 Summarize'
  },
  {
    'guid' : '49cd2c20-098a-46d9-9e47-6bf109308793',
    'id' : '3.5.1.2.8',
    'code' : '3.5.1.2.8',
    'abbr' : '3.5.1.2.8',
    'name' : { 'en': 'Emphasize' },
    'description' : 'Use this domain for words related to emphasizing something--to say something in a way that other people know that you think this thing is important.',
    'value' : '3.5.1.2.8 Emphasize'
  },
  {
    'guid' : '06b23bcd-69df-471a-b5a5-4ca8cab7f0d9',
    'id' : '3.5.1.2.9',
    'code' : '3.5.1.2.9',
    'abbr' : '3.5.1.2.9',
    'name' : { 'en': 'Be about, subject' },
    'description' : 'Use this domain for words that express the idea that something (said, written, thought, or made) depicts or is about a subject, or that something is logically related to some topic. Also use this domain for words that mark the topic or subject of what is being thought about, talked about, or written about. Verbs of thinking, knowing, or speaking (including other types of expression) can take a "topic" role. We can think of a "topic" as the main idea. Use this domain also for the important thing in a picture.',
    'value' : '3.5.1.2.9 Be about, subject'
  },
  {
    'guid' : 'ac2e424b-6aee-4031-8864-7b3f4a6fb5a3',
    'id' : '3.5.1.3',
    'code' : '3.5.1.3',
    'abbr' : '3.5.1.3',
    'name' : { 'en': 'True' },
    'description' : 'Use this domain for words that indicate if something is true, if it agrees with reality, or if it is not true.',
    'value' : '3.5.1.3 True'
  },
  {
    'guid' : '3bc961d1-9f4f-4f1b-ada7-b1e9a2928ea4',
    'id' : '3.5.1.3.1',
    'code' : '3.5.1.3.1',
    'abbr' : '3.5.1.3.1',
    'name' : { 'en': 'Tell the truth' },
    'description' : 'Use this domain for words related to telling the truth.',
    'value' : '3.5.1.3.1 Tell the truth'
  },
  {
    'guid' : '9f0bcab1-8256-47a1-853c-408f025e04e7',
    'id' : '3.5.1.3.2',
    'code' : '3.5.1.3.2',
    'abbr' : '3.5.1.3.2',
    'name' : { 'en': 'Tell a lie' },
    'description' : 'Use this domain for words related to lying--to say something that is not true.',
    'value' : '3.5.1.3.2 Tell a lie'
  },
  {
    'guid' : '02b6da2b-fae3-49f4-83f0-fd014024e117',
    'id' : '3.5.1.3.3',
    'code' : '3.5.1.3.3',
    'abbr' : '3.5.1.3.3',
    'name' : { 'en': 'Contradict' },
    'description' : 'Use this domain for words related to contradicting someone--to say that something someone has said is not true.',
    'value' : '3.5.1.3.3 Contradict'
  },
  {
    'guid' : '016e3f5b-b527-446e-9da6-49af34870001',
    'id' : '3.5.1.3.4',
    'code' : '3.5.1.3.4',
    'abbr' : '3.5.1.3.4',
    'name' : { 'en': 'Expose falsehood' },
    'description' : 'Use this domain for words related to exposing falsehood--to do something to show that someone has told a lie.',
    'value' : '3.5.1.3.4 Expose falsehood'
  },
  {
    'guid' : '51d9d243-35cc-4a1e-bcdd-f2749975f5fd',
    'id' : '3.5.1.3.5',
    'code' : '3.5.1.3.5',
    'abbr' : '3.5.1.3.5',
    'name' : { 'en': 'Real' },
    'description' : 'Use this domain for words that indicate if something is real.',
    'value' : '3.5.1.3.5 Real'
  },
  {
    'guid' : 'ab8ca07c-b23c-43dc-b705-7fe34ddf6d4d',
    'id' : '3.5.1.3.6',
    'code' : '3.5.1.3.6',
    'abbr' : '3.5.1.3.6',
    'name' : { 'en': 'Exaggerate' },
    'description' : 'Use this domain for words referring to exaggerating--reporting information but saying something untrue that makes the information seem bigger or more important than it really is.',
    'value' : '3.5.1.3.6 Exaggerate'
  },
  {
    'guid' : 'ec1bcace-fc10-45df-8e1f-29bce1ef786a',
    'id' : '3.5.1.4',
    'code' : '3.5.1.4',
    'abbr' : '3.5.1.4',
    'name' : { 'en': 'Speak with others' },
    'description' : 'Use this domain for words referring to carrying on a conversation with other people.',
    'value' : '3.5.1.4 Speak with others'
  },
  {
    'guid' : '14a32765-81b0-411e-89fa-91e092a70818',
    'id' : '3.5.1.4.1',
    'code' : '3.5.1.4.1',
    'abbr' : '3.5.1.4.1',
    'name' : { 'en': 'Call' },
    'description' : 'Use this domain for words related to calling someone--to say something loud because you want someone who is far away to listen to you.',
    'value' : '3.5.1.4.1 Call'
  },
  {
    'guid' : 'e033ca92-ee8c-4ab9-9368-5f6f4e942987',
    'id' : '3.5.1.4.2',
    'code' : '3.5.1.4.2',
    'abbr' : '3.5.1.4.2',
    'name' : { 'en': 'Contact' },
    'description' : 'Use this domain for words related to contacting someone--to communicate with someone who is far away from you using some communication device such as a telephone.',
    'value' : '3.5.1.4.2 Contact'
  },
  {
    'guid' : '52b04e15-7062-4fb2-9eaa-4fe8726f302a',
    'id' : '3.5.1.4.3',
    'code' : '3.5.1.4.3',
    'abbr' : '3.5.1.4.3',
    'name' : { 'en': 'Greet' },
    'description' : 'Use this domain for words related to greeting someone.',
    'value' : '3.5.1.4.3 Greet'
  },
  {
    'guid' : 'a9625460-7162-447c-b400-84fbc5744f1b',
    'id' : '3.5.1.4.4',
    'code' : '3.5.1.4.4',
    'abbr' : '3.5.1.4.4',
    'name' : { 'en': 'Say farewell' },
    'description' : 'Use this domain for words related to saying farewell.',
    'value' : '3.5.1.4.4 Say farewell'
  },
  {
    'guid' : '45b9bf61-3138-4206-9478-b4d3f082358b',
    'id' : '3.5.1.4.5',
    'code' : '3.5.1.4.5',
    'abbr' : '3.5.1.4.5',
    'name' : { 'en': 'Speak in unison' },
    'description' : 'Use this domain for words related to speaking in unison--to say something at the same time as someone else.',
    'value' : '3.5.1.4.5 Speak in unison'
  },
  {
    'guid' : '77b4d6c1-87bf-4839-b4be-6a45119b700a',
    'id' : '3.5.1.5',
    'code' : '3.5.1.5',
    'abbr' : '3.5.1.5',
    'name' : { 'en': 'Ask' },
    'description' : 'Use this domain for words related to asking a question.',
    'value' : '3.5.1.5 Ask'
  },
  {
    'guid' : '5fdf3946-7e47-4fd2-906f-4da7ce5fa490',
    'id' : '3.5.1.5.1',
    'code' : '3.5.1.5.1',
    'abbr' : '3.5.1.5.1',
    'name' : { 'en': 'Answer' },
    'description' : 'Use this domain for words related to answering a question.',
    'value' : '3.5.1.5.1 Answer'
  },
  {
    'guid' : 'a30e0391-ea64-4938-9eca-023c351d60af',
    'id' : '3.5.1.5.2',
    'code' : '3.5.1.5.2',
    'abbr' : '3.5.1.5.2',
    'name' : { 'en': 'Disclose' },
    'description' : 'Use this domain for words that refer to discovering and revealing unknown information.',
    'value' : '3.5.1.5.2 Disclose'
  },
  {
    'guid' : '16dbd62c-f60d-4530-ba4e-0e74221e4681',
    'id' : '3.5.1.5.3',
    'code' : '3.5.1.5.3',
    'abbr' : '3.5.1.5.3',
    'name' : { 'en': 'Hide your thoughts' },
    'description' : 'Use this domain for words related to hiding your thoughts.',
    'value' : '3.5.1.5.3 Hide your thoughts'
  },
  {
    'guid' : 'eafdea8e-521e-4614-8fd5-e8446adf9203',
    'id' : '3.5.1.6',
    'code' : '3.5.1.6',
    'abbr' : '3.5.1.6',
    'name' : { 'en': 'Debate' },
    'description' : 'Use this domain for words referring to debating--for two or more people to discuss some issue and try to persuade the other person to accept one"s view.',
    'value' : '3.5.1.6 Debate'
  },
  {
    'guid' : 'ebac5ec8-dc4c-4b2b-a727-3ca82404cdbb',
    'id' : '3.5.1.6.1',
    'code' : '3.5.1.6.1',
    'abbr' : '3.5.1.6.1',
    'name' : { 'en': 'Demonstrate' },
    'description' : 'Use this domain for words referring to demonstrating something--to do something that shows the truth of a statement, or shows how to do something.',
    'value' : '3.5.1.6.1 Demonstrate'
  },
  {
    'guid' : 'e5020b79-6fb0-4be4-a359-d4f899da5c7e',
    'id' : '3.5.1.6.2',
    'code' : '3.5.1.6.2',
    'abbr' : '3.5.1.6.2',
    'name' : { 'en': 'Quarrel' },
    'description' : 'Use this domain for words related to quarreling--to fight with words.',
    'value' : '3.5.1.6.2 Quarrel'
  },
  {
    'guid' : 'a4958dd9-03cc-4863-ab76-cd0682060cb0',
    'id' : '3.5.1.7',
    'code' : '3.5.1.7',
    'abbr' : '3.5.1.7',
    'name' : { 'en': 'Praise' },
    'description' : 'Use this domain for words related to praising someone or something--to say something good about someone, or to say that someone did something good.',
    'value' : '3.5.1.7 Praise'
  },
  {
    'guid' : '27048124-c204-4585-9997-c51728f085d6',
    'id' : '3.5.1.7.1',
    'code' : '3.5.1.7.1',
    'abbr' : '3.5.1.7.1',
    'name' : { 'en': 'Thank' },
    'description' : 'Use this domain for words related to thanking someone--to tell someone that you feel good about something they did for you.',
    'value' : '3.5.1.7.1 Thank'
  },
  {
    'guid' : '53d34f16-2f94-4afa-9530-7ac75e05b8d4',
    'id' : '3.5.1.7.2',
    'code' : '3.5.1.7.2',
    'abbr' : '3.5.1.7.2',
    'name' : { 'en': 'Flatter' },
    'description' : 'Use this domain for words referring to flattering someone--saying something nice to someone but not meaning it.',
    'value' : '3.5.1.7.2 Flatter'
  },
  {
    'guid' : '80415fac-b3d8-4e3c-bdb8-dc92f3c6dad8',
    'id' : '3.5.1.7.3',
    'code' : '3.5.1.7.3',
    'abbr' : '3.5.1.7.3',
    'name' : { 'en': 'Boast' },
    'description' : 'Use this domain for words related to boasting--to say something good about yourself, especially to make it seem that you are better than you really are.',
    'value' : '3.5.1.7.3 Boast'
  },
  {
    'guid' : 'bc9d763c-e4fe-48ab-ad44-87a36f6cc06f',
    'id' : '3.5.1.8',
    'code' : '3.5.1.8',
    'abbr' : '3.5.1.8',
    'name' : { 'en': 'Criticize' },
    'description' : 'Use this domain for words related to criticizing someone or something--to say that something is bad, especially something that someone did.',
    'value' : '3.5.1.8 Criticize'
  },
  {
    'guid' : 'c79d49f1-74ec-4dba-a5aa-5e861af63d05',
    'id' : '3.5.1.8.1',
    'code' : '3.5.1.8.1',
    'abbr' : '3.5.1.8.1',
    'name' : { 'en': 'Blame' },
    'description' : 'Use this domain for words related to blaming someone for something--to say that someone did something, and because of this something bad happened.',
    'value' : '3.5.1.8.1 Blame'
  },
  {
    'guid' : '81b62078-984c-4e82-94c4-39fa44dd3e56',
    'id' : '3.5.1.8.2',
    'code' : '3.5.1.8.2',
    'abbr' : '3.5.1.8.2',
    'name' : { 'en': 'Insult' },
    'description' : 'Use this domain for words related to insulting someone--to say that someone is bad.',
    'value' : '3.5.1.8.2 Insult'
  },
  {
    'guid' : 'e2d3294b-4463-48bb-95e4-8c9b5238ecec',
    'id' : '3.5.1.8.3',
    'code' : '3.5.1.8.3',
    'abbr' : '3.5.1.8.3',
    'name' : { 'en': 'Mock' },
    'description' : 'Use this domain for words referring to mocking someone--doing or saying something to make people laugh at someone because you don"t like them.',
    'value' : '3.5.1.8.3 Mock'
  },
  {
    'guid' : '7bca1201-31f8-4f65-8da9-af0eff36b388',
    'id' : '3.5.1.8.4',
    'code' : '3.5.1.8.4',
    'abbr' : '3.5.1.8.4',
    'name' : { 'en': 'Gossip' },
    'description' : 'Use this domain for words related to gossip--to say something bad about a person who is not with you.',
    'value' : '3.5.1.8.4 Gossip'
  },
  {
    'guid' : 'f3dbb078-6265-4861-a6e3-46cc151c5d72',
    'id' : '3.5.1.8.5',
    'code' : '3.5.1.8.5',
    'abbr' : '3.5.1.8.5',
    'name' : { 'en': 'Complain' },
    'description' : 'Use this domain for words related to complaining--to say that you don"t like something.',
    'value' : '3.5.1.8.5 Complain'
  },
  {
    'guid' : '3dba39bc-48f2-4bcb-9357-c8fbed6922ca',
    'id' : '3.5.1.9',
    'code' : '3.5.1.9',
    'abbr' : '3.5.1.9',
    'name' : { 'en': 'Promise' },
    'description' : 'Use this domain for words related to promising to do something--to say that you will do something in the future.',
    'value' : '3.5.1.9 Promise'
  },
  {
    'guid' : '05472990-3f51-40b3-bca8-df3cf383328b',
    'id' : '3.5.2',
    'code' : '3.5.2',
    'abbr' : '3.5.2',
    'name' : { 'en': 'Make speech' },
    'description' : 'Use this domain for words related to making a speech--to talk for a long time to many people.',
    'value' : '3.5.2 Make speech'
  },
  {
    'guid' : 'cc3f1dc8-a31e-4459-ba13-f82b45df37b5',
    'id' : '3.5.2.1',
    'code' : '3.5.2.1',
    'abbr' : '3.5.2.1',
    'name' : { 'en': 'Report' },
    'description' : 'Use this domain for words related to reporting something--to say that something has happened and to tell about it.',
    'value' : '3.5.2.1 Report'
  },
  {
    'guid' : 'f2342d42-bdc4-449c-9891-58f90318b9f1',
    'id' : '3.5.2.2',
    'code' : '3.5.2.2',
    'abbr' : '3.5.2.2',
    'name' : { 'en': 'News, message' },
    'description' : 'Use this domain for words referring to something someone says, which relays information from someone else, or about something someone has done.',
    'value' : '3.5.2.2 News, message'
  },
  {
    'guid' : '22e8f542-0ab1-4f25-af50-fd0d02917fda',
    'id' : '3.5.2.3',
    'code' : '3.5.2.3',
    'abbr' : '3.5.2.3',
    'name' : { 'en': 'Figurative' },
    'description' : 'Use this domain for words referring to figurative speech--saying something that is not meant to be understood literally (according to the normal meaning of each word), or saying something that compares one thing to another.',
    'value' : '3.5.2.3 Figurative'
  },
  {
    'guid' : '7158c621-c46e-4173-80c1-188f514a920f',
    'id' : '3.5.2.4',
    'code' : '3.5.2.4',
    'abbr' : '3.5.2.4',
    'name' : { 'en': 'Admit' },
    'description' : 'Use this domain for words referring to admitting something--saying that you have done wrong, or that your beliefs were wrong',
    'value' : '3.5.2.4 Admit'
  },
  {
    'guid' : 'bc96b3e3-6185-4925-b79e-8f0f9555bfb7',
    'id' : '3.5.3',
    'code' : '3.5.3',
    'abbr' : '3.5.3',
    'name' : { 'en': 'Language' },
    'description' : 'Use this domain for words referring to a language.',
    'value' : '3.5.3 Language'
  },
  {
    'guid' : '29a8ebbe-ebc4-4295-b6af-84331d019361',
    'id' : '3.5.3.1',
    'code' : '3.5.3.1',
    'abbr' : '3.5.3.1',
    'name' : { 'en': 'Word' },
    'description' : 'Use this domain for words that refer to words and groups of words.',
    'value' : '3.5.3.1 Word'
  },
  {
    'guid' : '4a388000-d5c6-4127-91cd-f4e0c9fac6f1',
    'id' : '3.5.3.2',
    'code' : '3.5.3.2',
    'abbr' : '3.5.3.2',
    'name' : { 'en': 'Information' },
    'description' : 'Use this domain for words referring to information--something someone says about something.',
    'value' : '3.5.3.2 Information'
  },
  {
    'guid' : '3160b7ad-e4e8-4a46-8e2e-d5e601969547',
    'id' : '3.5.4',
    'code' : '3.5.4',
    'abbr' : '3.5.4',
    'name' : { 'en': 'Story' },
    'description' : 'Use this domain for words referring to a story.',
    'value' : '3.5.4 Story'
  },
  {
    'guid' : 'f0404b23-db91-46c7-87e1-9f1be0712980',
    'id' : '3.5.4.1',
    'code' : '3.5.4.1',
    'abbr' : '3.5.4.1',
    'name' : { 'en': 'Fable, myth' },
    'description' : 'Use this domain for words referring to a fable or myth--a story that people tell that is not true.',
    'value' : '3.5.4.1 Fable, myth'
  },
  {
    'guid' : '65ef6aae-019f-4980-b6d9-5b7ad2c8e68f',
    'id' : '3.5.4.2',
    'code' : '3.5.4.2',
    'abbr' : '3.5.4.2',
    'name' : { 'en': 'Saying, proverb' },
    'description' : 'Use this domain for words referring to a saying or proverb--a short thing that people say to teach something.',
    'value' : '3.5.4.2 Saying, proverb'
  },
  {
    'guid' : '26d32f3e-ced6-45fc-afd0-7e017fa252c6',
    'id' : '3.5.4.3',
    'code' : '3.5.4.3',
    'abbr' : '3.5.4.3',
    'name' : { 'en': 'Riddle' },
    'description' : 'Use this domain for words referring to a riddle--something someone says that is hard to understand.',
    'value' : '3.5.4.3 Riddle'
  },
  {
    'guid' : 'f0f3c371-166e-4a66-849f-60d6fa7ad889',
    'id' : '3.5.4.4',
    'code' : '3.5.4.4',
    'abbr' : '3.5.4.4',
    'name' : { 'en': 'Poetry' },
    'description' : 'Use this domain for words related to poetry.',
    'value' : '3.5.4.4 Poetry'
  },
  {
    'guid' : '2629943b-3a69-4c6b-9956-2aa59ebd03d3',
    'id' : '3.5.4.5',
    'code' : '3.5.4.5',
    'abbr' : '3.5.4.5',
    'name' : { 'en': 'History' },
    'description' : 'Use this domain for words related to history.',
    'value' : '3.5.4.5 History'
  },
  {
    'guid' : 'a4fa9f98-73c6-4c3e-9dad-d73d2634be3b',
    'id' : '3.5.4.6',
    'code' : '3.5.4.6',
    'abbr' : '3.5.4.6',
    'name' : { 'en': 'Verbal tradition' },
    'description' : 'Use this domain for words referring to verbal tradition--something that your ancestors told to each successive generation.',
    'value' : '3.5.4.6 Verbal tradition'
  },
  {
    'guid' : 'f4b77866-c607-43f0-b816-95459c269525',
    'id' : '3.5.5',
    'code' : '3.5.5',
    'abbr' : '3.5.5',
    'name' : { 'en': 'Foolish talk' },
    'description' : 'Use this domain for words related to foolish talk--something someone says that other people think is silly or stupid.',
    'value' : '3.5.5 Foolish talk'
  },
  {
    'guid' : '59a95939-44d7-4396-9ef7-3a80c17e9fb1',
    'id' : '3.5.5.1',
    'code' : '3.5.5.1',
    'abbr' : '3.5.5.1',
    'name' : { 'en': 'Obscenity' },
    'description' : 'Use this domain for obscene words related to sex, defecation, death, damnation, and other taboo or offensive subjects. It is important to check the appropriateness of including obscene words in the dictionary. It should be the choice of the speakers of the language whether a word should be included or excluded. Obscene words included in the dictionary should be clearly marked as such. Obscenity is often used to heighten the emotion of an expression, and is often used when someone is angry. Obscenity is often associated with sex or defecation. Cursing is often associated with God, religion, or the afterlife. Insults often involve equating a person with an animal that is associated with an undesirable characteristic, questioning someone"s parentage/legitimacy, questioning someone"s character, or questioning someone"s intelligence/sanity.',
    'value' : '3.5.5.1 Obscenity'
  },
  {
    'guid' : '767aa167-af3d-4e11-a68b-ec31f9d2ef1a',
    'id' : '3.5.6',
    'code' : '3.5.6',
    'abbr' : '3.5.6',
    'name' : { 'en': 'Sign, symbol' },
    'description' : 'Use this domain for words referring to a sign or symbol--a picture or shape that has a meaning.',
    'value' : '3.5.6 Sign, symbol'
  },
  {
    'guid' : '96a1ad48-1a70-425b-bd20-59294902581f',
    'id' : '3.5.6.1',
    'code' : '3.5.6.1',
    'abbr' : '3.5.6.1',
    'name' : { 'en': 'Gesture' },
    'description' : 'Use this domain for words referring to gesturing--moving a part of the body to communicate a message.',
    'value' : '3.5.6.1 Gesture'
  },
  {
    'guid' : 'ef876104-eb3e-420d-9c7b-124538a7b2a6',
    'id' : '3.5.6.2',
    'code' : '3.5.6.2',
    'abbr' : '3.5.6.2',
    'name' : { 'en': 'Point at' },
    'description' : 'Use this domain for words related to pointing at something--to move a part of your body toward something so that people will look at it.',
    'value' : '3.5.6.2 Point at'
  },
  {
    'guid' : '339f54a5-125b-435f-bf37-cfc2a2bd26d3',
    'id' : '3.5.6.3',
    'code' : '3.5.6.3',
    'abbr' : '3.5.6.3',
    'name' : { 'en': 'Facial expression' },
    'description' : 'Use this domain for facial expressions--ways in which people move the parts of their faces to show feeling or communicate something.',
    'value' : '3.5.6.3 Facial expression'
  },
  {
    'guid' : 'cbc0a8f5-9cba-41d2-a7e5-565fbf09c8c4',
    'id' : '3.5.6.4',
    'code' : '3.5.6.4',
    'abbr' : '3.5.6.4',
    'name' : { 'en': 'Laugh' },
    'description' : 'Use this domain for words related to the expression of good feelings, including laughing--the sounds a person makes when he is enjoying himself or thinks something is funny.',
    'value' : '3.5.6.4 Laugh'
  },
  {
    'guid' : 'b4fe4698-54a2-4bcd-9490-e07ee1ee97af',
    'id' : '3.5.6.5',
    'code' : '3.5.6.5',
    'abbr' : '3.5.6.5',
    'name' : { 'en': 'Cry, tear' },
    'description' : 'Use this domain for words related to crying--when water forms in the eyes because of sadness or pain.',
    'value' : '3.5.6.5 Cry, tear'
  },
  {
    'guid' : '70eac6be-66e8-4827-8f2f-d15427efff60',
    'id' : '3.5.7',
    'code' : '3.5.7',
    'abbr' : '3.5.7',
    'name' : { 'en': 'Reading and writing' },
    'description' : 'Use this domain for general words related to reading and writing.',
    'value' : '3.5.7 Reading and writing'
  },
  {
    'guid' : 'af700054-258a-458a-9e38-e90397833e51',
    'id' : '3.5.7.1',
    'code' : '3.5.7.1',
    'abbr' : '3.5.7.1',
    'name' : { 'en': 'Write' },
    'description' : 'Use this domain for words related to writing.',
    'value' : '3.5.7.1 Write'
  },
  {
    'guid' : '8db7c016-bdc9-410b-9523-3197602358f4',
    'id' : '3.5.7.2',
    'code' : '3.5.7.2',
    'abbr' : '3.5.7.2',
    'name' : { 'en': 'Written material' },
    'description' : 'Use this domain for words referring to written material--something that has writing on it.',
    'value' : '3.5.7.2 Written material'
  },
  {
    'guid' : '710828bc-5dfb-4685-b1a5-156700ab08f1',
    'id' : '3.5.7.3',
    'code' : '3.5.7.3',
    'abbr' : '3.5.7.3',
    'name' : { 'en': 'Read' },
    'description' : 'Use this domain for words related to reading.',
    'value' : '3.5.7.3 Read'
  },
  {
    'guid' : '50ac28ab-7385-408f-b5eb-3e27b191fcf4',
    'id' : '3.5.7.4',
    'code' : '3.5.7.4',
    'abbr' : '3.5.7.4',
    'name' : { 'en': 'Publish' },
    'description' : 'Use this domain for words related to publishing books.',
    'value' : '3.5.7.4 Publish'
  },
  {
    'guid' : 'ba8d18bd-2556-47a0-aa33-3ebef3e90814',
    'id' : '3.5.7.5',
    'code' : '3.5.7.5',
    'abbr' : '3.5.7.5',
    'name' : { 'en': 'Record' },
    'description' : 'Use this domain for words related to a record--something written because people need to remember it.',
    'value' : '3.5.7.5 Record'
  },
  {
    'guid' : 'b7801f6e-683b-4d5d-9bab-57f6e593db8c',
    'id' : '3.5.7.6',
    'code' : '3.5.7.6',
    'abbr' : '3.5.7.6',
    'name' : { 'en': 'List' },
    'description' : 'Use this domain for words related to a list of things.',
    'value' : '3.5.7.6 List'
  },
  {
    'guid' : 'd586a164-ac8f-4356-8aa8-07721c2b5e09',
    'id' : '3.5.7.7',
    'code' : '3.5.7.7',
    'abbr' : '3.5.7.7',
    'name' : { 'en': 'Letter' },
    'description' : 'Use this domain for words referring to letter--a written message that is sent to someone.',
    'value' : '3.5.7.7 Letter'
  },
  {
    'guid' : 'c4330001-83ca-485d-8b9b-09f7e1be60cc',
    'id' : '3.5.8',
    'code' : '3.5.8',
    'abbr' : '3.5.8',
    'name' : { 'en': 'Interpreting messages' },
    'description' : 'Use this domain for words referring to interpreting something someone says--to try to understand the meaning of something someone says and express it in other words.',
    'value' : '3.5.8 Interpreting messages'
  },
  {
    'guid' : '8894cac9-c82a-4616-856e-0516d2ed1df7',
    'id' : '3.5.8.1',
    'code' : '3.5.8.1',
    'abbr' : '3.5.8.1',
    'name' : { 'en': 'Meaning' },
    'description' : 'Use this domain for words related to the meaning of something that is said.',
    'value' : '3.5.8.1 Meaning'
  },
  {
    'guid' : 'edf17f72-7e7a-4f8d-a5ee-f4889492d73a',
    'id' : '3.5.8.2',
    'code' : '3.5.8.2',
    'abbr' : '3.5.8.2',
    'name' : { 'en': 'Meaningless' },
    'description' : 'Use this domain for words describing something that is meaningless--something someone says that doesn"t mean anything.',
    'value' : '3.5.8.2 Meaningless'
  },
  {
    'guid' : '751bd45c-abfb-443f-ac55-aad3472c20de',
    'id' : '3.5.8.3',
    'code' : '3.5.8.3',
    'abbr' : '3.5.8.3',
    'name' : { 'en': 'Unintelligible' },
    'description' : 'Use this domain for words related to being unintelligible--to say something that someone cannot understand.',
    'value' : '3.5.8.3 Unintelligible'
  },
  {
    'guid' : '8225de87-35a3-4c7a-b35c-f45b152caebe',
    'id' : '3.5.8.4',
    'code' : '3.5.8.4',
    'abbr' : '3.5.8.4',
    'name' : { 'en': 'Show, indicate' },
    'description' : 'Use this domain for words referring to showing or indicating something--if something (such as an object, something said, or something that happens) shows something, it makes people think of something or understand something (e.g. When his face turns red, it indicates he is angry.)',
    'value' : '3.5.8.4 Show, indicate'
  },
  {
    'guid' : 'cac1d7a8-7382-466e-ba4a-ba2bfe50f13b',
    'id' : '3.5.9',
    'code' : '3.5.9',
    'abbr' : '3.5.9',
    'name' : { 'en': 'Mass communication' },
    'description' : 'Use this domain for words related to radio, television, newspapers, magazines and other forms of mass communication.',
    'value' : '3.5.9 Mass communication'
  },
  {
    'guid' : 'eef8c50e-c391-482c-9f60-1bba2d8892b3',
    'id' : '3.5.9.1',
    'code' : '3.5.9.1',
    'abbr' : '3.5.9.1',
    'name' : { 'en': 'Radio, television' },
    'description' : 'Use this domain for words related to radio and television.',
    'value' : '3.5.9.1 Radio, television'
  },
  {
    'guid' : '32bf055a-d666-4d6e-a3c6-6c984e2c9868',
    'id' : '3.5.9.2',
    'code' : '3.5.9.2',
    'abbr' : '3.5.9.2',
    'name' : { 'en': 'Telephone' },
    'description' : 'Use this domain for words related to the telephone.',
    'value' : '3.5.9.2 Telephone'
  },
  {
    'guid' : '12781062-ee36-4703-9bc0-cee4ed467ee5',
    'id' : '3.5.9.3',
    'code' : '3.5.9.3',
    'abbr' : '3.5.9.3',
    'name' : { 'en': 'Newspaper' },
    'description' : 'Use this domain for words related to newspapers and magazines.',
    'value' : '3.5.9.3 Newspaper'
  },
  {
    'guid' : '4f516445-e044-4d9c-ac9b-a3178f72b405',
    'id' : '3.5.9.4',
    'code' : '3.5.9.4',
    'abbr' : '3.5.9.4',
    'name' : { 'en': 'Movie' },
    'description' : 'Use this domain for words related to movies and the cinema.',
    'value' : '3.5.9.4 Movie'
  },
  {
    'guid' : '80dc5ca1-44ce-4406-add8-2bbe19c122ab',
    'id' : '3.5.9.5',
    'code' : '3.5.9.5',
    'abbr' : '3.5.9.5',
    'name' : { 'en': 'Recorded music' },
    'description' : 'Use this domain for words related to recording music.',
    'value' : '3.5.9.5 Recorded music'
  },
  {
    'guid' : '8d266c98-9db3-4d3e-b204-956aa848ffa5',
    'id' : '3.5.9.6',
    'code' : '3.5.9.6',
    'abbr' : '3.5.9.6',
    'name' : { 'en': 'Communication devices' },
    'description' : 'Use this domain for words related to communication devices.',
    'value' : '3.5.9.6 Communication devices'
  },
  {
    'guid' : '6137239a-b469-46be-b7cb-b9ac22fcc195',
    'id' : '3.6',
    'code' : '3.6',
    'abbr' : '3.6',
    'name' : { 'en': 'Teach' },
    'description' : 'Use this domain for words related to teaching.',
    'value' : '3.6 Teach'
  },
  {
    'guid' : '4093bfe8-54b3-4ffc-bfe3-3999279840b5',
    'id' : '3.6.1',
    'code' : '3.6.1',
    'abbr' : '3.6.1',
    'name' : { 'en': 'Show, explain' },
    'description' : 'Use this domain for words related to showing someone how something works, or explaining something to someone.',
    'value' : '3.6.1 Show, explain'
  },
  {
    'guid' : '3fdba5e5-eb24-4b2f-a6fc-d1ed7397c39c',
    'id' : '3.6.2',
    'code' : '3.6.2',
    'abbr' : '3.6.2',
    'name' : { 'en': 'School' },
    'description' : 'Use this domain for words related to school.',
    'value' : '3.6.2 School'
  },
  {
    'guid' : '1fa683b9-78fd-4feb-9978-55d5953f38ec',
    'id' : '3.6.3',
    'code' : '3.6.3',
    'abbr' : '3.6.3',
    'name' : { 'en': 'Subject of teaching' },
    'description' : 'Use this domain for words related to a subject that is taught or a subject that you study at school.',
    'value' : '3.6.3 Subject of teaching'
  },
  {
    'guid' : 'e4bacc52-dcaa-4e68-b736-f0b5d9aeca41',
    'id' : '3.6.4',
    'code' : '3.6.4',
    'abbr' : '3.6.4',
    'name' : { 'en': 'Class, lesson' },
    'description' : 'Use this domain for words related to a class--the period of time when a subject is taught.',
    'value' : '3.6.4 Class, lesson'
  },
  {
    'guid' : '87dd09b8-689c-4a56-b3f6-846a849f71b8',
    'id' : '3.6.5',
    'code' : '3.6.5',
    'abbr' : '3.6.5',
    'name' : { 'en': 'Correct' },
    'description' : 'Use this domain for words related to correcting a mistake.',
    'value' : '3.6.5 Correct'
  },
  {
    'guid' : '1886ffc9-0a18-41ea-b2f6-c17c297f1681',
    'id' : '3.6.6',
    'code' : '3.6.6',
    'abbr' : '3.6.6',
    'name' : { 'en': 'Science' },
    'description' : 'Use this domain for words related to science.',
    'value' : '3.6.6 Science'
  },
  {
    'guid' : '1688280e-27c4-47a8-87b7-8fe31b174ab8',
    'id' : '3.6.7',
    'code' : '3.6.7',
    'abbr' : '3.6.7',
    'name' : { 'en': 'Test' },
    'description' : 'Use this domain for words related to a test.',
    'value' : '3.6.7 Test'
  },
  {
    'guid' : '51d4e258-430c-4032-94e3-ee53095e7045',
    'id' : '3.6.8',
    'code' : '3.6.8',
    'abbr' : '3.6.8',
    'name' : { 'en': 'Answer in a test' },
    'description' : 'Use this domain for words related to the answer to a question in a test.',
    'value' : '3.6.8 Answer in a test'
  },
  {
    'guid' : '62b4ae33-f3c2-447a-9ef7-7e41805b6a02',
    'id' : '4',
    'code' : '4',
    'abbr' : '4',
    'name' : { 'en': 'Social behavior' },
    'description' : 'Use this domain for very general words having to do with how people behave in relationship to other people.',
    'value' : '4 Social behavior'
  },
  {
    'guid' : '0e79435b-f5ff-4061-81ff-49557ba2aed4',
    'id' : '4.1',
    'code' : '4.1',
    'abbr' : '4.1',
    'name' : { 'en': 'Relationships' },
    'description' : 'Use this domain for words related to relationships between people and groups of people.',
    'value' : '4.1 Relationships'
  },
  {
    'guid' : '646dab64-5c2f-4f45-8e28-4d13437639d4',
    'id' : '4.1.1',
    'code' : '4.1.1',
    'abbr' : '4.1.1',
    'name' : { 'en': 'Friend' },
    'description' : 'Use this domain for words related to friendship.',
    'value' : '4.1.1 Friend'
  },
  {
    'guid' : 'b40637e5-be76-4a82-96bb-c55306ee293d',
    'id' : '4.1.1.1',
    'code' : '4.1.1.1',
    'abbr' : '4.1.1.1',
    'name' : { 'en': 'Girlfriend, boyfriend' },
    'description' : 'Use this domain for words referring to a girlfriend or boyfriend.',
    'value' : '4.1.1.1 Girlfriend, boyfriend'
  },
  {
    'guid' : '995751ef-d71b-429c-a4ee-032f5b309bd7',
    'id' : '4.1.2',
    'code' : '4.1.2',
    'abbr' : '4.1.2',
    'name' : { 'en': 'Types of people' },
    'description' : 'Use this domain for words referring to different types of people.',
    'value' : '4.1.2 Types of people'
  },
  {
    'guid' : '513771eb-8467-468a-8bc8-e52567e66df9',
    'id' : '4.1.2.1',
    'code' : '4.1.2.1',
    'abbr' : '4.1.2.1',
    'name' : { 'en': 'Working relationship' },
    'description' : 'Use this domain for words related to a relationship between people who work together.',
    'value' : '4.1.2.1 Working relationship'
  },
  {
    'guid' : '469b0a30-3c26-4cfd-b948-7bb952eeff41',
    'id' : '4.1.3',
    'code' : '4.1.3',
    'abbr' : '4.1.3',
    'name' : { 'en': 'Know someone' },
    'description' : 'Use this domain for words referring to knowing someone.',
    'value' : '4.1.3 Know someone'
  },
  {
    'guid' : 'b5d679e3-506a-4994-81a2-be48a698d945',
    'id' : '4.1.3.1',
    'code' : '4.1.3.1',
    'abbr' : '4.1.3.1',
    'name' : { 'en': 'Meet for the first time' },
    'description' : 'Use this domain for words related to meeting someone for the first time.',
    'value' : '4.1.3.1 Meet for the first time'
  },
  {
    'guid' : 'd49f2c9d-1b4a-4370-b107-71f9b9fbdc8e',
    'id' : '4.1.4',
    'code' : '4.1.4',
    'abbr' : '4.1.4',
    'name' : { 'en': 'Neighbor' },
    'description' : 'Use this domain for words referring to a neighbor--someone who lives nearby.',
    'value' : '4.1.4 Neighbor'
  },
  {
    'guid' : 'aec8c246-0ef7-414a-94a6-b9fdd6812a7c',
    'id' : '4.1.4.1',
    'code' : '4.1.4.1',
    'abbr' : '4.1.4.1',
    'name' : { 'en': 'Social class' },
    'description' : 'Use this domain for words related to social class.',
    'value' : '4.1.4.1 Social class'
  },
  {
    'guid' : '9ac6dec6-1b8f-463c-8239-e2acb93586b1',
    'id' : '4.1.5',
    'code' : '4.1.5',
    'abbr' : '4.1.5',
    'name' : { 'en': 'Unity' },
    'description' : 'Use this domain for words related to unity--when a group of people agree with each other and are not fighting.',
    'value' : '4.1.5 Unity'
  },
  {
    'guid' : 'd59a84e1-5e12-4cb7-b72b-15c51810ad48',
    'id' : '4.1.6',
    'code' : '4.1.6',
    'abbr' : '4.1.6',
    'name' : { 'en': 'Disunity' },
    'description' : 'Use this domain for words related to disunity--when a group of people do not agree with each other and are fighting.',
    'value' : '4.1.6 Disunity'
  },
  {
    'guid' : 'eaed8c63-9f97-4116-927c-19f364a99e72',
    'id' : '4.1.6.1',
    'code' : '4.1.6.1',
    'abbr' : '4.1.6.1',
    'name' : { 'en': 'Unfriendly' },
    'description' : 'Use this domain for words related to being antisocial--when someone does not want to talk to other people or be friends with them.',
    'value' : '4.1.6.1 Unfriendly'
  },
  {
    'guid' : 'a8b2abb3-d09f-4ff1-89ce-6ae99f16401c',
    'id' : '4.1.6.2',
    'code' : '4.1.6.2',
    'abbr' : '4.1.6.2',
    'name' : { 'en': 'Set self apart' },
    'description' : 'Use this domain for words related to setting yourself apart from other people and not relating to them.',
    'value' : '4.1.6.2 Set self apart'
  },
  {
    'guid' : '4651aef1-e18f-481e-9c3e-d9dfff4a6b51',
    'id' : '4.1.6.3',
    'code' : '4.1.6.3',
    'abbr' : '4.1.6.3',
    'name' : { 'en': 'Alone' },
    'description' : 'Use this domain for words related to being alone.',
    'value' : '4.1.6.3 Alone'
  },
  {
    'guid' : '0ac81210-20ff-4a89-948f-5d154668f05c',
    'id' : '4.1.6.4',
    'code' : '4.1.6.4',
    'abbr' : '4.1.6.4',
    'name' : { 'en': 'Independent person' },
    'description' : 'Use this domain for words describing an independent person--someone who does things without other people"s help.',
    'value' : '4.1.6.4 Independent person'
  },
  {
    'guid' : 'c223335b-4803-4f1b-bf4d-f1ee077513cf',
    'id' : '4.1.6.5',
    'code' : '4.1.6.5',
    'abbr' : '4.1.6.5',
    'name' : { 'en': 'Private, public' },
    'description' : 'Use this domain for words describing something that is private--something that only concerns you, and for words describing something that is public--something that concerns many people.',
    'value' : '4.1.6.5 Private, public'
  },
  {
    'guid' : '675d67bb-da64-456e-8825-bdf074bb82be',
    'id' : '4.1.7',
    'code' : '4.1.7',
    'abbr' : '4.1.7',
    'name' : { 'en': 'Begin a relationship' },
    'description' : 'Use this domain for words related to beginning a relationship.',
    'value' : '4.1.7 Begin a relationship'
  },
  {
    'guid' : '06341e45-c407-49d0-98d8-74ecc303fb02',
    'id' : '4.1.7.1',
    'code' : '4.1.7.1',
    'abbr' : '4.1.7.1',
    'name' : { 'en': 'End a relationship' },
    'description' : 'Use this domain for words related to ending a relationship.',
    'value' : '4.1.7.1 End a relationship'
  },
  {
    'guid' : 'aeb20093-26ba-492b-a69d-af18d5ba51eb',
    'id' : '4.1.8',
    'code' : '4.1.8',
    'abbr' : '4.1.8',
    'name' : { 'en': 'Show affection' },
    'description' : 'Use this domain for words related to showing affection.',
    'value' : '4.1.8 Show affection'
  },
  {
    'guid' : 'e7c58c11-2911-446a-96b0-2113247f3792',
    'id' : '4.1.9',
    'code' : '4.1.9',
    'abbr' : '4.1.9',
    'name' : { 'en': 'Kinship' },
    'description' : 'Use this domain for general words for family relationships, not for specific terms.',
    'value' : '4.1.9 Kinship'
  },
  {
    'guid' : '529140d1-6e8e-44fe-99f0-95289e933607',
    'id' : '4.1.9.1',
    'code' : '4.1.9.1',
    'abbr' : '4.1.9.1',
    'name' : { 'en': 'Related by birth' },
    'description' : 'Use this domain for words referring to being related by birth--when one of your ancestors and one of someone else"s ancestors are the same person.',
    'value' : '4.1.9.1 Related by birth'
  },
  {
    'guid' : 'd1e58469-52e3-4b50-b0de-00bf9f09f8d4',
    'id' : '4.1.9.1.1',
    'code' : '4.1.9.1.1',
    'abbr' : '4.1.9.1.1',
    'name' : { 'en': 'Grandfather, grandmother' },
    'description' : 'Use this domain for words referring to grandparents and ancestors.',
    'value' : '4.1.9.1.1 Grandfather, grandmother'
  },
  {
    'guid' : '44dc42e4-e9c7-4aa9-ac9b-1008385244b1',
    'id' : '4.1.9.1.2',
    'code' : '4.1.9.1.2',
    'abbr' : '4.1.9.1.2',
    'name' : { 'en': 'Father, mother' },
    'description' : 'Use this domain for words related to parents.',
    'value' : '4.1.9.1.2 Father, mother'
  },
  {
    'guid' : 'bb29001e-97f3-4bb4-8946-7c33b9835fcb',
    'id' : '4.1.9.1.3',
    'code' : '4.1.9.1.3',
    'abbr' : '4.1.9.1.3',
    'name' : { 'en': 'Brother, sister' },
    'description' : 'Use this domain for words related to siblings.',
    'value' : '4.1.9.1.3 Brother, sister'
  },
  {
    'guid' : '87c499b3-5fab-45e0-9999-9c4fcbba1e2b',
    'id' : '4.1.9.1.4',
    'code' : '4.1.9.1.4',
    'abbr' : '4.1.9.1.4',
    'name' : { 'en': 'Son, daughter' },
    'description' : 'Use this domain for words referring to your children.',
    'value' : '4.1.9.1.4 Son, daughter'
  },
  {
    'guid' : 'f47d681b-4f0d-43ca-a465-2e1724825752',
    'id' : '4.1.9.1.5',
    'code' : '4.1.9.1.5',
    'abbr' : '4.1.9.1.5',
    'name' : { 'en': 'Grandson, granddaughter' },
    'description' : 'Use this domain for words referring to your grandchildren.',
    'value' : '4.1.9.1.5 Grandson, granddaughter'
  },
  {
    'guid' : '75202262-cdba-4c43-9343-764c84138797',
    'id' : '4.1.9.1.6',
    'code' : '4.1.9.1.6',
    'abbr' : '4.1.9.1.6',
    'name' : { 'en': 'Uncle, aunt' },
    'description' : 'Use this domain for words related to your uncles and aunts.',
    'value' : '4.1.9.1.6 Uncle, aunt'
  },
  {
    'guid' : 'f957a4aa-d3d4-4dad-93d1-20565b5158d4',
    'id' : '4.1.9.1.7',
    'code' : '4.1.9.1.7',
    'abbr' : '4.1.9.1.7',
    'name' : { 'en': 'Cousin' },
    'description' : 'Use this domain for words referring to your cousins.',
    'value' : '4.1.9.1.7 Cousin'
  },
  {
    'guid' : 'eb0c9e02-e4c1-4e5e-84b6-be63aaf439d5',
    'id' : '4.1.9.1.8',
    'code' : '4.1.9.1.8',
    'abbr' : '4.1.9.1.8',
    'name' : { 'en': 'Nephew, niece' },
    'description' : 'Use this domain for words referring to your nephews and nieces.',
    'value' : '4.1.9.1.8 Nephew, niece'
  },
  {
    'guid' : '89944377-8694-4394-bced-153cc22a0e30',
    'id' : '4.1.9.1.9',
    'code' : '4.1.9.1.9',
    'abbr' : '4.1.9.1.9',
    'name' : { 'en': 'Birth order' },
    'description' : 'Use this domain for words related to birth order.',
    'value' : '4.1.9.1.9 Birth order'
  },
  {
    'guid' : '49bc0d75-4f07-44b4-a50f-bc9a557dc15e',
    'id' : '4.1.9.2',
    'code' : '4.1.9.2',
    'abbr' : '4.1.9.2',
    'name' : { 'en': 'Related by marriage' },
    'description' : 'Use this domain for words referring to being related by marriage.',
    'value' : '4.1.9.2 Related by marriage'
  },
  {
    'guid' : 'a4627ca8-f27b-44ce-91f5-cc992332bc86',
    'id' : '4.1.9.2.1',
    'code' : '4.1.9.2.1',
    'abbr' : '4.1.9.2.1',
    'name' : { 'en': 'Husband, wife' },
    'description' : 'Use this domain for words related to your spouse.',
    'value' : '4.1.9.2.1 Husband, wife'
  },
  {
    'guid' : '89ad4e41-bf08-4d93-a4f3-f72e8cc62bed',
    'id' : '4.1.9.2.2',
    'code' : '4.1.9.2.2',
    'abbr' : '4.1.9.2.2',
    'name' : { 'en': 'In-law' },
    'description' : 'Use this domain for words referring to being related by marriage.',
    'value' : '4.1.9.2.2 In-law'
  },
  {
    'guid' : '04370e1f-25aa-4d9e-97c5-de9b59156666',
    'id' : '4.1.9.3',
    'code' : '4.1.9.3',
    'abbr' : '4.1.9.3',
    'name' : { 'en': 'Widow, widower' },
    'description' : 'Use this domain for words referring to widows and widowers. Some languages also have words for a parent who has lost a child, someone who has lost a brother or sister, or a general word for someone who has lost a relative.',
    'value' : '4.1.9.3 Widow, widower'
  },
  {
    'guid' : '57237095-23cf-43ba-aa6c-89cecdd35ff8',
    'id' : '4.1.9.4',
    'code' : '4.1.9.4',
    'abbr' : '4.1.9.4',
    'name' : { 'en': 'Orphan' },
    'description' : 'Use this domain for words referring to orphans.',
    'value' : '4.1.9.4 Orphan'
  },
  {
    'guid' : '0656f6a7-9a88-4c03-a8ab-ace8c0f52ebf',
    'id' : '4.1.9.5',
    'code' : '4.1.9.5',
    'abbr' : '4.1.9.5',
    'name' : { 'en': 'Illegitimate child' },
    'description' : 'Use this domain for words referring to an illegitimate child.',
    'value' : '4.1.9.5 Illegitimate child'
  },
  {
    'guid' : '4405e74c-f64c-4609-8f7b-99ba563d659a',
    'id' : '4.1.9.6',
    'code' : '4.1.9.6',
    'abbr' : '4.1.9.6',
    'name' : { 'en': 'Adopt' },
    'description' : 'Use this domain for words related to adopting a child.',
    'value' : '4.1.9.6 Adopt'
  },
  {
    'guid' : 'fa41dbc5-adbb-4ad0-9fd2-0278d4689a28',
    'id' : '4.1.9.7',
    'code' : '4.1.9.7',
    'abbr' : '4.1.9.7',
    'name' : { 'en': 'Non-relative' },
    'description' : 'Use this domain for words that refer to people who are not related by blood or marriage.',
    'value' : '4.1.9.7 Non-relative'
  },
  {
    'guid' : 'a781d57d-174d-47a6-b6a1-ae635a13df84',
    'id' : '4.1.9.8',
    'code' : '4.1.9.8',
    'abbr' : '4.1.9.8',
    'name' : { 'en': 'Family, clan' },
    'description' : 'Use this domain for words that refer to social groups composed of related people.',
    'value' : '4.1.9.8 Family, clan'
  },
  {
    'guid' : '6cbdaf94-8e2c-4b26-936a-d2f86d158250',
    'id' : '4.1.9.9',
    'code' : '4.1.9.9',
    'abbr' : '4.1.9.9',
    'name' : { 'en': 'Race' },
    'description' : 'Use this domain for words referring to different races of people--the large groups of people in the world that are different in color and appearance.',
    'value' : '4.1.9.9 Race'
  },
  {
    'guid' : '57225f57-ba51-45d7-b6d2-b22052877ea4',
    'id' : '4.2',
    'code' : '4.2',
    'abbr' : '4.2',
    'name' : { 'en': 'Social activity' },
    'description' : 'Use this domain for general words referring to social activities.',
    'value' : '4.2 Social activity'
  },
  {
    'guid' : '935da513-126e-4cab-8e07-625458821181',
    'id' : '4.2.1',
    'code' : '4.2.1',
    'abbr' : '4.2.1',
    'name' : { 'en': 'Come together, form a group' },
    'description' : 'Use this domain for words referring to coming together to form a group.',
    'value' : '4.2.1 Come together, form a group'
  },
  {
    'guid' : '7459c0d8-4da1-4944-a95e-bc64cde860f5',
    'id' : '4.2.1.1',
    'code' : '4.2.1.1',
    'abbr' : '4.2.1.1',
    'name' : { 'en': 'Invite' },
    'description' : 'Use this domain for words referring to inviting people to meet together--to say something to someone because you want to meet with them.',
    'value' : '4.2.1.1 Invite'
  },
  {
    'guid' : '6709cc78-cb0b-493e-b3e6-8590a2f20c95',
    'id' : '4.2.1.2',
    'code' : '4.2.1.2',
    'abbr' : '4.2.1.2',
    'name' : { 'en': 'Encounter' },
    'description' : 'Use this domain for words referring to encountering someone.',
    'value' : '4.2.1.2 Encounter'
  },
  {
    'guid' : 'c358b041-7a1a-43d6-8e61-26b9507f559f',
    'id' : '4.2.1.3',
    'code' : '4.2.1.3',
    'abbr' : '4.2.1.3',
    'name' : { 'en': 'Meet together' },
    'description' : 'Use this domain for words related to meeting together.',
    'value' : '4.2.1.3 Meet together'
  },
  {
    'guid' : '4f19ab95-428a-4a0b-a069-ca8be6b72b08',
    'id' : '4.2.1.4',
    'code' : '4.2.1.4',
    'abbr' : '4.2.1.4',
    'name' : { 'en': 'Visit' },
    'description' : 'Use this domain for words related to visiting someone.',
    'value' : '4.2.1.4 Visit'
  },
  {
    'guid' : '252886c4-9317-4c6b-a69e-13520eb89736',
    'id' : '4.2.1.4.1',
    'code' : '4.2.1.4.1',
    'abbr' : '4.2.1.4.1',
    'name' : { 'en': 'Welcome, receive' },
    'description' : 'Use this domain for words related to welcoming a visitor.',
    'value' : '4.2.1.4.1 Welcome, receive'
  },
  {
    'guid' : '17851b86-f8fb-4850-9b33-c1a9fcb0aec1',
    'id' : '4.2.1.4.2',
    'code' : '4.2.1.4.2',
    'abbr' : '4.2.1.4.2',
    'name' : { 'en': 'Show hospitality' },
    'description' : 'Use this domain for words related to showing hospitality to a visitor.',
    'value' : '4.2.1.4.2 Show hospitality'
  },
  {
    'guid' : '54b6dff4-a21d-490d-8279-69f36a179c93',
    'id' : '4.2.1.5',
    'code' : '4.2.1.5',
    'abbr' : '4.2.1.5',
    'name' : { 'en': 'Meeting, assembly' },
    'description' : 'Use this domain for words referring to a meeting.',
    'value' : '4.2.1.5 Meeting, assembly'
  },
  {
    'guid' : 'efd03c89-bf8b-4d46-a921-06cc06f28356',
    'id' : '4.2.1.6',
    'code' : '4.2.1.6',
    'abbr' : '4.2.1.6',
    'name' : { 'en': 'Participate' },
    'description' : 'Use this domain for words related to participating in a group--to do things with a group.',
    'value' : '4.2.1.6 Participate'
  },
  {
    'guid' : '87eb572c-bae2-45d8-a36a-919561b55d0d',
    'id' : '4.2.1.7',
    'code' : '4.2.1.7',
    'abbr' : '4.2.1.7',
    'name' : { 'en': 'Crowd, group' },
    'description' : 'Use this domain for words referring to a crowd or group of people.',
    'value' : '4.2.1.7 Crowd, group'
  },
  {
    'guid' : '86f90eff-158b-4f6d-82e9-fab136dfd141',
    'id' : '4.2.1.8',
    'code' : '4.2.1.8',
    'abbr' : '4.2.1.8',
    'name' : { 'en': 'Organization' },
    'description' : 'Use this domain for words referring to an organization.',
    'value' : '4.2.1.8 Organization'
  },
  {
    'guid' : 'e3a6f918-4b0f-4515-bd6c-4f3370bcbf67',
    'id' : '4.2.1.8.1',
    'code' : '4.2.1.8.1',
    'abbr' : '4.2.1.8.1',
    'name' : { 'en': 'Join an organization' },
    'description' : 'Use this domain for words related to joining an organization.',
    'value' : '4.2.1.8.1 Join an organization'
  },
  {
    'guid' : 'e173f481-ec57-4d1c-b517-be38ccb038f5',
    'id' : '4.2.1.8.2',
    'code' : '4.2.1.8.2',
    'abbr' : '4.2.1.8.2',
    'name' : { 'en': 'Leave an organization' },
    'description' : 'Use this domain for words related to leaving an organization.',
    'value' : '4.2.1.8.2 Leave an organization'
  },
  {
    'guid' : '92ef1096-bae2-4bc5-b4b8-c16f429c4867',
    'id' : '4.2.1.8.3',
    'code' : '4.2.1.8.3',
    'abbr' : '4.2.1.8.3',
    'name' : { 'en': 'Belong to an organization' },
    'description' : 'Use this domain for words related to belonging to an organization.',
    'value' : '4.2.1.8.3 Belong to an organization'
  },
  {
    'guid' : 'f81b7632-3e5a-4a2d-8a93-648872a6616b',
    'id' : '4.2.1.9',
    'code' : '4.2.1.9',
    'abbr' : '4.2.1.9',
    'name' : { 'en': 'Social group' },
    'description' : 'Use this domain for words referring to an organization.',
    'value' : '4.2.1.9 Social group'
  },
  {
    'guid' : 'c4b110cf-d968-4bc6-ac0c-7e70cbad2756',
    'id' : '4.2.2',
    'code' : '4.2.2',
    'abbr' : '4.2.2',
    'name' : { 'en': 'Social event' },
    'description' : 'Use this domain for words referring to a social event.',
    'value' : '4.2.2 Social event'
  },
  {
    'guid' : '71cbefc3-e0a5-4b97-9672-fa0cb34c59e4',
    'id' : '4.2.2.1',
    'code' : '4.2.2.1',
    'abbr' : '4.2.2.1',
    'name' : { 'en': 'Ceremony' },
    'description' : 'Use this domain for words referring to a ceremony.',
    'value' : '4.2.2.1 Ceremony'
  },
  {
    'guid' : 'ddc96103-4bc5-44d3-9412-c57569d2a9f5',
    'id' : '4.2.2.2',
    'code' : '4.2.2.2',
    'abbr' : '4.2.2.2',
    'name' : { 'en': 'Festival, show' },
    'description' : 'Use this domain for words referring to a festival or show--a large social event during which some people entertain other people.',
    'value' : '4.2.2.2 Festival, show'
  },
  {
    'guid' : 'aff720bd-fb3d-4f85-bbc4-41d5fc5b83f8',
    'id' : '4.2.2.3',
    'code' : '4.2.2.3',
    'abbr' : '4.2.2.3',
    'name' : { 'en': 'Celebrate' },
    'description' : 'Use this domain for words related to celebrating.',
    'value' : '4.2.2.3 Celebrate'
  },
  {
    'guid' : '5446b5cf-f05a-4bb2-89eb-ce63e27040f3',
    'id' : '4.2.3',
    'code' : '4.2.3',
    'abbr' : '4.2.3',
    'name' : { 'en': 'Music' },
    'description' : 'Use this domain for words related to music.',
    'value' : '4.2.3 Music'
  },
  {
    'guid' : 'aa0f165d-3013-4a0c-b68c-14ea317013f9',
    'id' : '4.2.3.1',
    'code' : '4.2.3.1',
    'abbr' : '4.2.3.1',
    'name' : { 'en': 'Compose music' },
    'description' : 'Use this domain for words related to composing music.',
    'value' : '4.2.3.1 Compose music'
  },
  {
    'guid' : '7fed6281-326a-4a15-8cbf-dc574594da19',
    'id' : '4.2.3.2',
    'code' : '4.2.3.2',
    'abbr' : '4.2.3.2',
    'name' : { 'en': 'Play music' },
    'description' : 'Use this domain for words related to playing music.',
    'value' : '4.2.3.2 Play music'
  },
  {
    'guid' : 'de6b15d3-6409-4998-b03d-903133f4ad70',
    'id' : '4.2.3.3',
    'code' : '4.2.3.3',
    'abbr' : '4.2.3.3',
    'name' : { 'en': 'Sing' },
    'description' : 'Use this domain for words related to singing.',
    'value' : '4.2.3.3 Sing'
  },
  {
    'guid' : '0a1b26b2-2152-45e2-9b63-4a68fca73a90',
    'id' : '4.2.3.4',
    'code' : '4.2.3.4',
    'abbr' : '4.2.3.4',
    'name' : { 'en': 'Musician' },
    'description' : 'Use this domain for words related to a musician.',
    'value' : '4.2.3.4 Musician'
  },
  {
    'guid' : '91ddf495-a28b-4a32-90dc-6f997d0cd069',
    'id' : '4.2.3.5',
    'code' : '4.2.3.5',
    'abbr' : '4.2.3.5',
    'name' : { 'en': 'Musical instrument' },
    'description' : 'Use this domain for words referring to musical instruments and people who play a particular instrument.',
    'value' : '4.2.3.5 Musical instrument'
  },
  {
    'guid' : '4b3a9b5a-df7d-4ec2-9576-2c07fe396021',
    'id' : '4.2.4',
    'code' : '4.2.4',
    'abbr' : '4.2.4',
    'name' : { 'en': 'Dance' },
    'description' : 'Use this domain for words related to dancing.',
    'value' : '4.2.4 Dance'
  },
  {
    'guid' : 'e27adda9-0761-4b7f-abbf-24938ce1c01a',
    'id' : '4.2.5',
    'code' : '4.2.5',
    'abbr' : '4.2.5',
    'name' : { 'en': 'Drama' },
    'description' : 'Use this domain for words related to drama.',
    'value' : '4.2.5 Drama'
  },
  {
    'guid' : '6745c89d-1fcd-44b8-be4b-9fd9d62f64e7',
    'id' : '4.2.6',
    'code' : '4.2.6',
    'abbr' : '4.2.6',
    'name' : { 'en': 'Entertainment, recreation' },
    'description' : 'Use this domain for words related to entertainment and recreation.',
    'value' : '4.2.6 Entertainment, recreation'
  },
  {
    'guid' : '721e7782-9add-41fa-a7cf-659d5ca33926',
    'id' : '4.2.6.1',
    'code' : '4.2.6.1',
    'abbr' : '4.2.6.1',
    'name' : { 'en': 'Game' },
    'description' : 'Use this domain for words referring to games. Some languages do not make a distinction between "Games" and "Sports."',
    'value' : '4.2.6.1 Game'
  },
  {
    'guid' : 'ac4a936e-6c05-4e73-8d40-dfe4223b47fa',
    'id' : '4.2.6.1.1',
    'code' : '4.2.6.1.1',
    'abbr' : '4.2.6.1.1',
    'name' : { 'en': 'Card game' },
    'description' : 'Use this domain for words related to a particular game. The example words are from the card games. If you do not play card games in your culture, you can rename this domain and use it for one of your games. Add other domains for each of your games.',
    'value' : '4.2.6.1.1 Card game'
  },
  {
    'guid' : '2d92e248-1512-4e89-b886-425814c6dd32',
    'id' : '4.2.6.1.2',
    'code' : '4.2.6.1.2',
    'abbr' : '4.2.6.1.2',
    'name' : { 'en': 'Chess' },
    'description' : 'Use this domain for words related to a particular game. The example words are from the game of chess. If you do not play chess in your culture, you can rename this domain and use it for one of your games. Add other domains for each of your games.',
    'value' : '4.2.6.1.2 Chess'
  },
  {
    'guid' : '01441207-4935-49a5-a192-16d949f5606c',
    'id' : '4.2.6.2',
    'code' : '4.2.6.2',
    'abbr' : '4.2.6.2',
    'name' : { 'en': 'Sports' },
    'description' : 'Use this domain for words related to sports.',
    'value' : '4.2.6.2 Sports'
  },
  {
    'guid' : '6187e620-89bd-4b9d-9896-0c0c788e7740',
    'id' : '4.2.6.2.1',
    'code' : '4.2.6.2.1',
    'abbr' : '4.2.6.2.1',
    'name' : { 'en': 'Football, soccer' },
    'description' : 'Use this domain for words related to a particular sport. The example words are from the sport of football (also called soccer). If you do not play football in your culture, you can rename this domain and use it for one of your sports. Add other domains for each of your sports.',
    'value' : '4.2.6.2.1 Football, soccer'
  },
  {
    'guid' : '21ebc64a-a1b8-45bd-b7f6-143a053f1d31',
    'id' : '4.2.6.2.2',
    'code' : '4.2.6.2.2',
    'abbr' : '4.2.6.2.2',
    'name' : { 'en': 'Basketball' },
    'description' : 'Use this domain for words related to a particular sport. The example words are from the sport of basketball. If you do not play basketball in your culture, you can rename this domain and use it for one of your sports. Add other domains for each of your sports.',
    'value' : '4.2.6.2.2 Basketball'
  },
  {
    'guid' : '28e874fb-b2e7-4afa-a4d7-600306ad2583',
    'id' : '4.2.6.3',
    'code' : '4.2.6.3',
    'abbr' : '4.2.6.3',
    'name' : { 'en': 'Exercise' },
    'description' : 'Use this domain for words related to exercise.',
    'value' : '4.2.6.3 Exercise'
  },
  {
    'guid' : '42133f78-9860-4bb7-8083-5559083f0714',
    'id' : '4.2.6.4',
    'code' : '4.2.6.4',
    'abbr' : '4.2.6.4',
    'name' : { 'en': 'Gambling' },
    'description' : 'Use this domain for words related to gambling.',
    'value' : '4.2.6.4 Gambling'
  },
  {
    'guid' : 'a3d95c6a-4b0b-4af0-aca8-addd042becd2',
    'id' : '4.2.7',
    'code' : '4.2.7',
    'abbr' : '4.2.7',
    'name' : { 'en': 'Play, fun' },
    'description' : 'Use this domain for words related to playing and fun.',
    'value' : '4.2.7 Play, fun'
  },
  {
    'guid' : 'c27b87cf-1211-4df2-96b1-12c416edabbe',
    'id' : '4.2.8',
    'code' : '4.2.8',
    'abbr' : '4.2.8',
    'name' : { 'en': 'Humor' },
    'description' : 'Use this domain for words related to humor.',
    'value' : '4.2.8 Humor'
  },
  {
    'guid' : '37a08f65-5a79-4e17-8e19-0975d6531d64',
    'id' : '4.2.8.1',
    'code' : '4.2.8.1',
    'abbr' : '4.2.8.1',
    'name' : { 'en': 'Serious' },
    'description' : 'Use this domain for words related to being serious--not laughing or joking.',
    'value' : '4.2.8.1 Serious'
  },
  {
    'guid' : '4e791773-94c8-4667-93f8-92dc0100ddfe',
    'id' : '4.2.9',
    'code' : '4.2.9',
    'abbr' : '4.2.9',
    'name' : { 'en': 'Holiday' },
    'description' : 'Use this domain for words related to a holiday.',
    'value' : '4.2.9 Holiday'
  },
  {
    'guid' : 'e791df50-8880-4080-a5ee-d4e58bb7b8ca',
    'id' : '4.2.9.1',
    'code' : '4.2.9.1',
    'abbr' : '4.2.9.1',
    'name' : { 'en': 'Free time' },
    'description' : 'Use this domain for words related to a holiday.',
    'value' : '4.2.9.1 Free time'
  },
  {
    'guid' : '8e11dd10-459b-4fb3-b876-7d80ec5f8a4d',
    'id' : '4.3',
    'code' : '4.3',
    'abbr' : '4.3',
    'name' : { 'en': 'Behavior' },
    'description' : 'Use this domain for general words referring to a person"s behavior.',
    'value' : '4.3 Behavior'
  },
  {
    'guid' : '30e6b42c-6bbf-4659-99e7-aa4b8e68c9ce',
    'id' : '4.3.1',
    'code' : '4.3.1',
    'abbr' : '4.3.1',
    'name' : { 'en': 'Good, moral' },
    'description' : 'Use this domain for words related to being good or moral in behavior.',
    'value' : '4.3.1 Good, moral'
  },
  {
    'guid' : '744d1402-05f5-4491-9c15-a5af03595edb',
    'id' : '4.3.1.1',
    'code' : '4.3.1.1',
    'abbr' : '4.3.1.1',
    'name' : { 'en': 'Bad, immoral' },
    'description' : 'Use this domain for words related to being bad or immoral in behavior, and for words describing a bad person.',
    'value' : '4.3.1.1 Bad, immoral'
  },
  {
    'guid' : 'a6797fd1-e368-422b-9710-96e0af39552d',
    'id' : '4.3.1.2',
    'code' : '4.3.1.2',
    'abbr' : '4.3.1.2',
    'name' : { 'en': 'Meet a standard' },
    'description' : 'Use this domain for words related to meeting a standard.',
    'value' : '4.3.1.2 Meet a standard'
  },
  {
    'guid' : '1461c106-d9e0-417d-9487-a57e6d0cced0',
    'id' : '4.3.1.2.1',
    'code' : '4.3.1.2.1',
    'abbr' : '4.3.1.2.1',
    'name' : { 'en': 'Below standard' },
    'description' : 'Use this domain for words related to behaving below a standard.',
    'value' : '4.3.1.2.1 Below standard'
  },
  {
    'guid' : '4260e110-7b04-4d40-9391-486a57aa3031',
    'id' : '4.3.1.3',
    'code' : '4.3.1.3',
    'abbr' : '4.3.1.3',
    'name' : { 'en': 'Mature in behavior' },
    'description' : 'Use this domain for words related to acting maturely--to act like an adult rather than a child.',
    'value' : '4.3.1.3 Mature in behavior'
  },
  {
    'guid' : '2b846476-00cf-4d82-97a1-26e1eda880ca',
    'id' : '4.3.1.3.1',
    'code' : '4.3.1.3.1',
    'abbr' : '4.3.1.3.1',
    'name' : { 'en': 'Immature in behavior' },
    'description' : 'Use this domain for words related to behaving in an immature way--for either a child or an adult to act like a child.',
    'value' : '4.3.1.3.1 Immature in behavior'
  },
  {
    'guid' : 'de1ffd73-af3b-47a2-8e98-ac1659a84cac',
    'id' : '4.3.1.3.2',
    'code' : '4.3.1.3.2',
    'abbr' : '4.3.1.3.2',
    'name' : { 'en': 'Sensible' },
    'description' : 'Use this domain for words related to being sensible--to think about what you do.',
    'value' : '4.3.1.3.2 Sensible'
  },
  {
    'guid' : 'cb783ad9-4650-416e-bf63-88c4ca43fe6a',
    'id' : '4.3.1.4',
    'code' : '4.3.1.4',
    'abbr' : '4.3.1.4',
    'name' : { 'en': 'Reputation' },
    'description' : 'Use this domain for words related to having a good reputation--when most people think well of someone because he does good things and doesn"t do bad things.',
    'value' : '4.3.1.4 Reputation'
  },
  {
    'guid' : 'afc25fbb-9060-4af2-8225-3fddbab2227d',
    'id' : '4.3.1.5',
    'code' : '4.3.1.5',
    'abbr' : '4.3.1.5',
    'name' : { 'en': 'Patient' },
    'description' : 'Use this domain for words related to being patient--if you are patient, you don"t get angry or do anything bad even though something bad happens to you for a long time.',
    'value' : '4.3.1.5 Patient'
  },
  {
    'guid' : '8d490e91-6383-45ea-a3d7-b9c950822f98',
    'id' : '4.3.1.5.1',
    'code' : '4.3.1.5.1',
    'abbr' : '4.3.1.5.1',
    'name' : { 'en': 'Impatient' },
    'description' : 'Use this domain for words related to being impatient--if you are impatient, you get angry or do something bad when something bad happens to you for a long time.',
    'value' : '4.3.1.5.1 Impatient'
  },
  {
    'guid' : '93489181-ad8c-4a18-8dbc-fd7a9c871126',
    'id' : '4.3.1.5.2',
    'code' : '4.3.1.5.2',
    'abbr' : '4.3.1.5.2',
    'name' : { 'en': 'Bad-tempered' },
    'description' : 'Use this domain for words related to being bad-tempered--to behave in an angry, unfriendly way.',
    'value' : '4.3.1.5.2 Bad-tempered'
  },
  {
    'guid' : '9b0fef63-5935-447d-801a-bb02dd6212bb',
    'id' : '4.3.2',
    'code' : '4.3.2',
    'abbr' : '4.3.2',
    'name' : { 'en': 'Admire someone' },
    'description' : 'Use this domain for words related to admiring someone--to feel good about someone because you think that he is a good person.',
    'value' : '4.3.2 Admire someone'
  },
  {
    'guid' : 'a2a3e21a-819c-4400-b2e6-e6c1a0a0ded3',
    'id' : '4.3.2.1',
    'code' : '4.3.2.1',
    'abbr' : '4.3.2.1',
    'name' : { 'en': 'Despise someone' },
    'description' : 'Use this domain for words related to despising someone--to feel bad about someone because you think they are not as good as you.',
    'value' : '4.3.2.1 Despise someone'
  },
  {
    'guid' : '5d21b3f1-85d5-4999-af64-c4d0101050c0',
    'id' : '4.3.2.2',
    'code' : '4.3.2.2',
    'abbr' : '4.3.2.2',
    'name' : { 'en': 'Humble' },
    'description' : 'Use this domain for words related to being humble--to not think that you are better than other people, or to not think that you are better than you really are, and to not talk or act as if you were better than other people.',
    'value' : '4.3.2.2 Humble'
  },
  {
    'guid' : '18b3ca02-18fe-4ab5-8709-c20957a0a2fb',
    'id' : '4.3.2.3',
    'code' : '4.3.2.3',
    'abbr' : '4.3.2.3',
    'name' : { 'en': 'Proud' },
    'description' : 'Use this domain for words referring to being proud--to think that something or someone is very good and to feel very good about them, especially to think and feel very good about yourself--to think that you are better than others, to think that you are better than you really are, or to talk and act as if you were better than other people.',
    'value' : '4.3.2.3 Proud'
  },
  {
    'guid' : '054e81ce-abd8-4069-989d-13e2fa58851c',
    'id' : '4.3.2.4',
    'code' : '4.3.2.4',
    'abbr' : '4.3.2.4',
    'name' : { 'en': 'Show off' },
    'description' : 'Use this domain for words referring to showing off--to behave in a way that attracts people"s attention because you want them to admire you.',
    'value' : '4.3.2.4 Show off'
  },
  {
    'guid' : 'e7caa24f-155d-47cd-946d-cc0d06dfc764',
    'id' : '4.3.3',
    'code' : '4.3.3',
    'abbr' : '4.3.3',
    'name' : { 'en': 'Love' },
    'description' : 'Use this domain for words related to loving someone--to feel good about someone, want good things to happen to them, and want to do good things for them.',
    'value' : '4.3.3 Love'
  },
  {
    'guid' : '642ff468-e6c8-4fd0-8f52-262efa8f7774',
    'id' : '4.3.3.1',
    'code' : '4.3.3.1',
    'abbr' : '4.3.3.1',
    'name' : { 'en': 'Hate, ill will' },
    'description' : 'Use this domain for words referring to hating someone--to want something bad to happen to someone, or to want to do something bad to someone.',
    'value' : '4.3.3.1 Hate, ill will'
  },
  {
    'guid' : 'b553e989-2b2a-4b1e-a987-ae75f3862501',
    'id' : '4.3.3.2',
    'code' : '4.3.3.2',
    'abbr' : '4.3.3.2',
    'name' : { 'en': 'Not care' },
    'description' : 'Use this domain for words related to not caring about someone.',
    'value' : '4.3.3.2 Not care'
  },
  {
    'guid' : '611aa361-4ddd-450b-a152-94984a274575',
    'id' : '4.3.3.3',
    'code' : '4.3.3.3',
    'abbr' : '4.3.3.3',
    'name' : { 'en': 'Abandon' },
    'description' : 'Use this domain for words related to abandoning someone or something--to leave and not return to them.',
    'value' : '4.3.3.3 Abandon'
  },
  {
    'guid' : '771e3882-f672-4e67-8580-edd82d7e5090',
    'id' : '4.3.4',
    'code' : '4.3.4',
    'abbr' : '4.3.4',
    'name' : { 'en': 'Do good to' },
    'description' : 'Use this domain for words referring to doing good to someone.',
    'value' : '4.3.4 Do good to'
  },
  {
    'guid' : 'ec79e90e-ecd3-497f-bc14-ac64181f53d7',
    'id' : '4.3.4.1',
    'code' : '4.3.4.1',
    'abbr' : '4.3.4.1',
    'name' : { 'en': 'Do evil to' },
    'description' : 'Use this domain for words related to doing evil to someone.',
    'value' : '4.3.4.1 Do evil to'
  },
  {
    'guid' : '87d344ac-94cc-49d6-9878-ebc86a933033',
    'id' : '4.3.4.2',
    'code' : '4.3.4.2',
    'abbr' : '4.3.4.2',
    'name' : { 'en': 'Help' },
    'description' : 'Use this domain for words related to helping someone.',
    'value' : '4.3.4.2 Help'
  },
  {
    'guid' : '60e63d4a-702e-4238-a42b-d97f6be09c29',
    'id' : '4.3.4.2.1',
    'code' : '4.3.4.2.1',
    'abbr' : '4.3.4.2.1',
    'name' : { 'en': 'Hinder' },
    'description' : 'Use this domain for words related to hindering someone.',
    'value' : '4.3.4.2.1 Hinder'
  },
  {
    'guid' : '50dfffe4-dfe8-445f-bdde-4cc8d83ebd6b',
    'id' : '4.3.4.3',
    'code' : '4.3.4.3',
    'abbr' : '4.3.4.3',
    'name' : { 'en': 'Cooperate with' },
    'description' : 'Use this domain for words related to cooperating with someone to do something.',
    'value' : '4.3.4.3 Cooperate with'
  },
  {
    'guid' : '92e441df-7e56-4e2c-b205-79a95800f567',
    'id' : '4.3.4.3.1',
    'code' : '4.3.4.3.1',
    'abbr' : '4.3.4.3.1',
    'name' : { 'en': 'Compete with' },
    'description' : 'Use this domain for words related to competing with someone.',
    'value' : '4.3.4.3.1 Compete with'
  },
  {
    'guid' : 'e6cf2c28-7630-41d7-835d-bd171ab67378',
    'id' : '4.3.4.4',
    'code' : '4.3.4.4',
    'abbr' : '4.3.4.4',
    'name' : { 'en': 'Altruistic, selfless' },
    'description' : 'Use this domain for words describing a person who acts in an altruistic, selfless manner--to act out of concern for the welfare of others without concern for your own welfare.',
    'value' : '4.3.4.4 Altruistic, selfless'
  },
  {
    'guid' : '5dcf3ce8-aa00-4478-b00e-691549fa29e8',
    'id' : '4.3.4.4.1',
    'code' : '4.3.4.4.1',
    'abbr' : '4.3.4.4.1',
    'name' : { 'en': 'Selfish' },
    'description' : 'Use this domain for words related to being selfish.',
    'value' : '4.3.4.4.1 Selfish'
  },
  {
    'guid' : 'a7e4f8a8-3fe3-4c86-85df-059f8983df26',
    'id' : '4.3.4.4.2',
    'code' : '4.3.4.4.2',
    'abbr' : '4.3.4.4.2',
    'name' : { 'en': 'Use a person' },
    'description' : 'Use this domain for words related to using someone for your own purpose or gain without helping them.',
    'value' : '4.3.4.4.2 Use a person'
  },
  {
    'guid' : 'b5b36c31-c56d-44b9-933c-fe0e62d80c25',
    'id' : '4.3.4.5',
    'code' : '4.3.4.5',
    'abbr' : '4.3.4.5',
    'name' : { 'en': 'Share with' },
    'description' : 'Use this domain for words related to sharing with other people.',
    'value' : '4.3.4.5 Share with'
  },
  {
    'guid' : '0f323bee-0d8a-4564-9691-87880f55d910',
    'id' : '4.3.4.5.1',
    'code' : '4.3.4.5.1',
    'abbr' : '4.3.4.5.1',
    'name' : { 'en': 'Provide for, support' },
    'description' : 'Use this domain for words referring to providing someone with what they need to live each day, such as providing for an elderly parent who can no longer earn what they need.',
    'value' : '4.3.4.5.1 Provide for, support'
  },
  {
    'guid' : '50e28fa7-f6c3-45bc-871d-12ef771d532c',
    'id' : '4.3.4.5.2',
    'code' : '4.3.4.5.2',
    'abbr' : '4.3.4.5.2',
    'name' : { 'en': 'Care for' },
    'description' : 'Use this domain for words related to caring for someone--to do something good for someone, because they need something and cannot do it.',
    'value' : '4.3.4.5.2 Care for'
  },
  {
    'guid' : '5d9ef67a-e4ba-47ca-8c8c-ea0f512a66cd',
    'id' : '4.3.4.5.3',
    'code' : '4.3.4.5.3',
    'abbr' : '4.3.4.5.3',
    'name' : { 'en': 'Entrust to the care of' },
    'description' : 'Use this domain for words related to entrusting something to the care of someone.',
    'value' : '4.3.4.5.3 Entrust to the care of'
  },
  {
    'guid' : 'fe66d433-5135-498e-a29d-b42bf0317252',
    'id' : '4.3.4.6',
    'code' : '4.3.4.6',
    'abbr' : '4.3.4.6',
    'name' : { 'en': 'Meddle' },
    'description' : 'Use this domain for words referring to meddling--involving oneself in something that does not concern oneself, such as someone else"s business or affairs, or a fight between other people.',
    'value' : '4.3.4.6 Meddle'
  },
  {
    'guid' : '57c9a370-0f46-4475-98fd-c7a8b3f5074c',
    'id' : '4.3.4.6.1',
    'code' : '4.3.4.6.1',
    'abbr' : '4.3.4.6.1',
    'name' : { 'en': 'Spoil' },
    'description' : 'Use this domain for words referring to spoiling something that is happening, such as an event, work, relationship, or feeling, by doing something that makes it less attractive, less enjoyable, or less effective--to do something bad to something that is happening, so that people can"t do what they were doing or don"t enjoy it as much.',
    'value' : '4.3.4.6.1 Spoil'
  },
  {
    'guid' : '9060339e-d697-4c35-bc83-ced6bebfee63',
    'id' : '4.3.4.7',
    'code' : '4.3.4.7',
    'abbr' : '4.3.4.7',
    'name' : { 'en': 'Enter by force' },
    'description' : 'Use this domain for words related to entering a place, such as a house, by force.',
    'value' : '4.3.4.7 Enter by force'
  },
  {
    'guid' : 'b0e5042d-1ade-4fb1-a6fd-9a165f5c4763',
    'id' : '4.3.4.8',
    'code' : '4.3.4.8',
    'abbr' : '4.3.4.8',
    'name' : { 'en': 'Kidnap' },
    'description' : 'Use this domain for words related to kidnapping someone.',
    'value' : '4.3.4.8 Kidnap'
  },
  {
    'guid' : '80b48f92-0a83-4eeb-bfe0-6980285feb65',
    'id' : '4.3.4.9',
    'code' : '4.3.4.9',
    'abbr' : '4.3.4.9',
    'name' : { 'en': 'Cruel' },
    'description' : 'Use this domain for words related to being cruel.',
    'value' : '4.3.4.9 Cruel'
  },
  {
    'guid' : '7d54d3dd-5fb2-4640-9bcf-c234696af894',
    'id' : '4.3.5',
    'code' : '4.3.5',
    'abbr' : '4.3.5',
    'name' : { 'en': 'Honest' },
    'description' : 'Use this domain for words related to being honest--words describing someone who does not cheat, steal, or break the law.',
    'value' : '4.3.5 Honest'
  },
  {
    'guid' : '5658ae3d-ea15-44db-bae4-47df792da12e',
    'id' : '4.3.5.1',
    'code' : '4.3.5.1',
    'abbr' : '4.3.5.1',
    'name' : { 'en': 'Dishonest' },
    'description' : 'Use this domain for words related to being dishonest.',
    'value' : '4.3.5.1 Dishonest'
  },
  {
    'guid' : 'eea7c79b-6150-4aba-8105-a94b7e6aeab7',
    'id' : '4.3.5.2',
    'code' : '4.3.5.2',
    'abbr' : '4.3.5.2',
    'name' : { 'en': 'Faithful' },
    'description' : 'Use this domain for words related to being faithful--to continue to love and do good to someone so that they can always trust you.',
    'value' : '4.3.5.2 Faithful'
  },
  {
    'guid' : '99ee1558-34b2-4a1b-bfd2-eb4ccbfe7728',
    'id' : '4.3.5.3',
    'code' : '4.3.5.3',
    'abbr' : '4.3.5.3',
    'name' : { 'en': 'Reliable' },
    'description' : 'Use this domain for words related to being reliable--words describe a person who can be relied on to do what he is supposed to.',
    'value' : '4.3.5.3 Reliable'
  },
  {
    'guid' : '81855f47-aa24-435d-a123-7dfe61c80702',
    'id' : '4.3.5.4',
    'code' : '4.3.5.4',
    'abbr' : '4.3.5.4',
    'name' : { 'en': 'Unreliable' },
    'description' : 'Use this domain for words related to being unreliable.',
    'value' : '4.3.5.4 Unreliable'
  },
  {
    'guid' : 'a730d0b4-b6db-45ac-bf6e-cefc96ae3fbb',
    'id' : '4.3.5.5',
    'code' : '4.3.5.5',
    'abbr' : '4.3.5.5',
    'name' : { 'en': 'Deceive' },
    'description' : 'Use this domain for words related to deceiving someone.',
    'value' : '4.3.5.5 Deceive'
  },
  {
    'guid' : '9bf21458-0acc-4d8d-ba9a-7b9fb6b8ee0b',
    'id' : '4.3.5.6',
    'code' : '4.3.5.6',
    'abbr' : '4.3.5.6',
    'name' : { 'en': 'Hypocrite' },
    'description' : 'Use this domain for words related to being a hypocrite.',
    'value' : '4.3.5.6 Hypocrite'
  },
  {
    'guid' : '8add7f4d-333b-4b2c-9999-48a14162152b',
    'id' : '4.3.6',
    'code' : '4.3.6',
    'abbr' : '4.3.6',
    'name' : { 'en': 'Self-controlled' },
    'description' : 'Use this domain for words referring to self-control--deciding not to do something that you want to do, because you think it would be bad to do it; or deciding to do something, because you know it is good.',
    'value' : '4.3.6 Self-controlled'
  },
  {
    'guid' : '3e546c11-bcb6-4024-b2f3-c15be40e257f',
    'id' : '4.3.6.1',
    'code' : '4.3.6.1',
    'abbr' : '4.3.6.1',
    'name' : { 'en': 'Lack self-control' },
    'description' : 'Use this domain for words related to a lack of self-control.',
    'value' : '4.3.6.1 Lack self-control'
  },
  {
    'guid' : '4acc430b-9c98-4a49-a8b4-15edc0f6d19b',
    'id' : '4.3.6.2',
    'code' : '4.3.6.2',
    'abbr' : '4.3.6.2',
    'name' : { 'en': 'Tidy' },
    'description' : 'Use this domain for words referring to being tidy in your habits, such as keeping your belongings neat and organized,.',
    'value' : '4.3.6.2 Tidy'
  },
  {
    'guid' : '9a9c7174-4148-43c2-875c-0d2f884a5fe3',
    'id' : '4.3.6.3',
    'code' : '4.3.6.3',
    'abbr' : '4.3.6.3',
    'name' : { 'en': 'Untidy' },
    'description' : 'Use this domain for words related to being untidy--to not keep your things tidy and orderly.',
    'value' : '4.3.6.3 Untidy'
  },
  {
    'guid' : 'cd8b2a8b-687b-42e9-91e7-cfb8812b64ee',
    'id' : '4.3.6.4',
    'code' : '4.3.6.4',
    'abbr' : '4.3.6.4',
    'name' : { 'en': 'Mistake' },
    'description' : 'Use this domain for words referring to mistakes--something bad that someone does by accident.',
    'value' : '4.3.6.4 Mistake'
  },
  {
    'guid' : '721e2ee8-7ea5-4d17-94cc-d77d8e92bd27',
    'id' : '4.3.7',
    'code' : '4.3.7',
    'abbr' : '4.3.7',
    'name' : { 'en': 'Polite' },
    'description' : 'Use this domain for words related to social refinement.',
    'value' : '4.3.7 Polite'
  },
  {
    'guid' : 'f4ec8f2e-f89e-40c1-ae50-900927d20af6',
    'id' : '4.3.7.1',
    'code' : '4.3.7.1',
    'abbr' : '4.3.7.1',
    'name' : { 'en': 'Impolite' },
    'description' : 'Use this domain for words related to being impolite.',
    'value' : '4.3.7.1 Impolite'
  },
  {
    'guid' : 'fc193988-26ca-49e9-849a-b12456d98792',
    'id' : '4.3.7.2',
    'code' : '4.3.7.2',
    'abbr' : '4.3.7.2',
    'name' : { 'en': 'Crazy' },
    'description' : 'Use this domain for words related to being crazy--to act stupid or strange.',
    'value' : '4.3.7.2 Crazy'
  },
  {
    'guid' : 'bce3f390-452c-4ca9-8c36-5fbcfd6b4755',
    'id' : '4.3.8',
    'code' : '4.3.8',
    'abbr' : '4.3.8',
    'name' : { 'en': 'Change behavior' },
    'description' : 'Use this domain for words related to changing your behavior either for the better or for the worse.',
    'value' : '4.3.8 Change behavior'
  },
  {
    'guid' : '0efe342d-4969-4bd1-95be-556f6c62adfc',
    'id' : '4.3.8.1',
    'code' : '4.3.8.1',
    'abbr' : '4.3.8.1',
    'name' : { 'en': 'Conform' },
    'description' : 'Use this domain for words related to conforming to the behavior of others--to try to behave the same way as other people, or to try to behave the way other people want you to act.',
    'value' : '4.3.8.1 Conform'
  },
  {
    'guid' : '73a59333-134f-4a1d-aba7-e134bdefe059',
    'id' : '4.3.9',
    'code' : '4.3.9',
    'abbr' : '4.3.9',
    'name' : { 'en': 'Culture' },
    'description' : 'Use this domain for general words referring to culture--the way a group of people (such as a tribe) behaves that is different from other groups.',
    'value' : '4.3.9 Culture'
  },
  {
    'guid' : 'f5567550-e3c9-4589-8f88-8159eadcd194',
    'id' : '4.3.9.1',
    'code' : '4.3.9.1',
    'abbr' : '4.3.9.1',
    'name' : { 'en': 'Custom' },
    'description' : 'Use this domain for words referring to customs--a particular practice of a cultural group.',
    'value' : '4.3.9.1 Custom'
  },
  {
    'guid' : '6a6bbf65-b521-4b74-bf35-dede87217d3c',
    'id' : '4.3.9.2',
    'code' : '4.3.9.2',
    'abbr' : '4.3.9.2',
    'name' : { 'en': 'Habit' },
    'description' : 'Use this domain for words referring to a habit--a pattern of behavior of a person; something a person does frequently or regularly.',
    'value' : '4.3.9.2 Habit'
  },
  {
    'guid' : '9bb173c6-cf38-47cd-803f-ebdf964ca2fc',
    'id' : '4.4',
    'code' : '4.4',
    'abbr' : '4.4',
    'name' : { 'en': 'Prosperity, trouble' },
    'description' : 'Use this domain for general words referring to prosperity and trouble',
    'value' : '4.4 Prosperity, trouble'
  },
  {
    'guid' : 'c1144a6e-3fce-4084-93d6-6f305eda8b1f',
    'id' : '4.4.1',
    'code' : '4.4.1',
    'abbr' : '4.4.1',
    'name' : { 'en': 'Prosperity' },
    'description' : 'Use this domain for words related to prosperity--when good things are happening.',
    'value' : '4.4.1 Prosperity'
  },
  {
    'guid' : '01459db0-bf2a-422b-8d55-0ab505aea2b4',
    'id' : '4.4.2',
    'code' : '4.4.2',
    'abbr' : '4.4.2',
    'name' : { 'en': 'Trouble' },
    'description' : 'Use this domain for words referring to bad things that happen in life.',
    'value' : '4.4.2 Trouble'
  },
  {
    'guid' : '4c823e93-3966-461f-bd64-3a9303966338',
    'id' : '4.4.2.1',
    'code' : '4.4.2.1',
    'abbr' : '4.4.2.1',
    'name' : { 'en': 'Problem' },
    'description' : 'Use this domain for words related to problems.',
    'value' : '4.4.2.1 Problem'
  },
  {
    'guid' : 'e0e83cc9-b876-47f6-8e66-60c9c505b927',
    'id' : '4.4.2.2',
    'code' : '4.4.2.2',
    'abbr' : '4.4.2.2',
    'name' : { 'en': 'Danger' },
    'description' : 'Use this domain for words referring to danger--things and events that threaten to inflict damage, pain, or death.',
    'value' : '4.4.2.2 Danger'
  },
  {
    'guid' : '9ec62ffe-69be-4b9b-944c-29a0f4f133db',
    'id' : '4.4.2.3',
    'code' : '4.4.2.3',
    'abbr' : '4.4.2.3',
    'name' : { 'en': 'Accident' },
    'description' : 'Use this domain for words related to accidents--something bad that happens without anyone wanting it to happen or doing anything to cause it to happen.',
    'value' : '4.4.2.3 Accident'
  },
  {
    'guid' : 'c8c8b1f6-a898-4d5b-a1ce-fa7284915e8f',
    'id' : '4.4.2.4',
    'code' : '4.4.2.4',
    'abbr' : '4.4.2.4',
    'name' : { 'en': 'Disaster' },
    'description' : 'Use this domain for words related to a disaster.',
    'value' : '4.4.2.4 Disaster'
  },
  {
    'guid' : '97885cab-cd96-4d34-a62a-3e3daac0c165',
    'id' : '4.4.2.5',
    'code' : '4.4.2.5',
    'abbr' : '4.4.2.5',
    'name' : { 'en': 'Separate, alone' },
    'description' : 'Use this domain for words referring to a person being separate or alone.',
    'value' : '4.4.2.5 Separate, alone'
  },
  {
    'guid' : '9a7604b4-d42f-44b7-9aef-47847dc93f0b',
    'id' : '4.4.2.6',
    'code' : '4.4.2.6',
    'abbr' : '4.4.2.6',
    'name' : { 'en': 'Suffer' },
    'description' : 'Use this domain for words related to suffering--to feel very bad because of something very bad that has happened to you.',
    'value' : '4.4.2.6 Suffer'
  },
  {
    'guid' : '65bb0844-9a71-4eec-9b86-bf4b4b508a28',
    'id' : '4.4.3',
    'code' : '4.4.3',
    'abbr' : '4.4.3',
    'name' : { 'en': 'Respond to trouble' },
    'description' : 'Use this domain for words related to responding to trouble.',
    'value' : '4.4.3 Respond to trouble'
  },
  {
    'guid' : '76a4a286-1c8a-4c4a-9eaf-dca040be574a',
    'id' : '4.4.3.1',
    'code' : '4.4.3.1',
    'abbr' : '4.4.3.1',
    'name' : { 'en': 'Brave' },
    'description' : 'Use this domain for words describing someone who is brave--not afraid to do something dangerous.',
    'value' : '4.4.3.1 Brave'
  },
  {
    'guid' : '401bbbe4-a33a-4a1e-b26a-18a756e002c4',
    'id' : '4.4.3.2',
    'code' : '4.4.3.2',
    'abbr' : '4.4.3.2',
    'name' : { 'en': 'Cowardice' },
    'description' : 'Use this domain for words related to feeling cowardly--to be afraid to do something because you think something bad might happen to you.',
    'value' : '4.4.3.2 Cowardice'
  },
  {
    'guid' : '20fadd54-6cec-4bb3-a47c-66c29aaff227',
    'id' : '4.4.3.3',
    'code' : '4.4.3.3',
    'abbr' : '4.4.3.3',
    'name' : { 'en': 'Avoid' },
    'description' : 'Use this domain for words related to avoiding something bad, such as trouble or someone you don"t want to meet.',
    'value' : '4.4.3.3 Avoid'
  },
  {
    'guid' : 'a90e47fb-667c-46f7-beab-5ec4e9e6edb5',
    'id' : '4.4.3.4',
    'code' : '4.4.3.4',
    'abbr' : '4.4.3.4',
    'name' : { 'en': 'Caution' },
    'description' : 'Use this domain for words related to being cautious.',
    'value' : '4.4.3.4 Caution'
  },
  {
    'guid' : '05811fbe-2361-4219-a5d3-be3dc487f6fa',
    'id' : '4.4.3.5',
    'code' : '4.4.3.5',
    'abbr' : '4.4.3.5',
    'name' : { 'en': 'Solve a problem' },
    'description' : 'Use this domain for words related to solving a problem.',
    'value' : '4.4.3.5 Solve a problem'
  },
  {
    'guid' : '2aabd548-5ee2-4962-8f10-84d1b0427c41',
    'id' : '4.4.3.6',
    'code' : '4.4.3.6',
    'abbr' : '4.4.3.6',
    'name' : { 'en': 'Endure' },
    'description' : 'Use this domain for words related to enduring a problem.',
    'value' : '4.4.3.6 Endure'
  },
  {
    'guid' : '7babf463-5e62-4a82-96d3-ef5989803c41',
    'id' : '4.4.3.7',
    'code' : '4.4.3.7',
    'abbr' : '4.4.3.7',
    'name' : { 'en': 'Survive' },
    'description' : 'Use this domain for words referring to surviving--to live through a time of danger.',
    'value' : '4.4.3.7 Survive'
  },
  {
    'guid' : '3dd684e6-75d9-41f4-aaa3-fb0cb3c7d400',
    'id' : '4.4.4',
    'code' : '4.4.4',
    'abbr' : '4.4.4',
    'name' : { 'en': 'Respond to someone in trouble' },
    'description' : 'Use this domain for words related to responding to someone in trouble.',
    'value' : '4.4.4 Respond to someone in trouble'
  },
  {
    'guid' : '51c2e2e4-438c-414b-bd15-773b664dd289',
    'id' : '4.4.4.1',
    'code' : '4.4.4.1',
    'abbr' : '4.4.4.1',
    'name' : { 'en': 'Mercy' },
    'description' : 'Use this domain for words related to having mercy on someone who has done something bad.',
    'value' : '4.4.4.1 Mercy'
  },
  {
    'guid' : '7ec98665-275f-4d57-8022-2740b9e90059',
    'id' : '4.4.4.2',
    'code' : '4.4.4.2',
    'abbr' : '4.4.4.2',
    'name' : { 'en': 'Show sympathy, support' },
    'description' : 'Use this domain for words related to showing sympathy and support to someone in trouble.',
    'value' : '4.4.4.2 Show sympathy, support'
  },
  {
    'guid' : '65926a7a-bc46-4a40-a2c9-bf84696a3903',
    'id' : '4.4.4.3',
    'code' : '4.4.4.3',
    'abbr' : '4.4.4.3',
    'name' : { 'en': 'Gentle' },
    'description' : 'Use this domain for words related to being gentle.',
    'value' : '4.4.4.3 Gentle'
  },
  {
    'guid' : '06ac577a-4d61-4898-ac9d-e3f18b7504af',
    'id' : '4.4.4.4',
    'code' : '4.4.4.4',
    'abbr' : '4.4.4.4',
    'name' : { 'en': 'Save from trouble' },
    'description' : 'Use this domain for words related to saving someone or something from trouble (something bad is happening) or danger (something bad will happen).',
    'value' : '4.4.4.4 Save from trouble'
  },
  {
    'guid' : '0a42fd83-3b30-4c85-bb68-f5132e9ffeee',
    'id' : '4.4.4.5',
    'code' : '4.4.4.5',
    'abbr' : '4.4.4.5',
    'name' : { 'en': 'Protect' },
    'description' : 'Use this domain for words related to protecting someone from danger or being hurt.',
    'value' : '4.4.4.5 Protect'
  },
  {
    'guid' : '6c282031-f1ca-492a-b94c-b48e74e6f25d',
    'id' : '4.4.4.6',
    'code' : '4.4.4.6',
    'abbr' : '4.4.4.6',
    'name' : { 'en': 'Free from bondage' },
    'description' : 'Use this domain for words related to freeing someone from bondage.',
    'value' : '4.4.4.6 Free from bondage'
  },
  {
    'guid' : '290f0994-ce8e-4922-975f-fa091f566823',
    'id' : '4.4.4.7',
    'code' : '4.4.4.7',
    'abbr' : '4.4.4.7',
    'name' : { 'en': 'Relief' },
    'description' : 'Use this domain for words related to relief.',
    'value' : '4.4.4.7 Relief'
  },
  {
    'guid' : '0eda983b-633e-4b11-b5c8-28be60067782',
    'id' : '4.4.4.8',
    'code' : '4.4.4.8',
    'abbr' : '4.4.4.8',
    'name' : { 'en': 'Risk' },
    'description' : 'Use this domain for words referring to risk--exposing oneself or something to danger.',
    'value' : '4.4.4.8 Risk'
  },
  {
    'guid' : 'b5499348-b8ca-4fae-8486-b23863560ae5',
    'id' : '4.4.5',
    'code' : '4.4.5',
    'abbr' : '4.4.5',
    'name' : { 'en': 'Chance' },
    'description' : 'Use this domain for words related to chance--when something happens that no one intended.',
    'value' : '4.4.5 Chance'
  },
  {
    'guid' : '99b40843-54bf-4c59-ae1d-d7146cebec48',
    'id' : '4.4.5.1',
    'code' : '4.4.5.1',
    'abbr' : '4.4.5.1',
    'name' : { 'en': 'Lucky' },
    'description' : 'Use this domain for words related to being lucky.',
    'value' : '4.4.5.1 Lucky'
  },
  {
    'guid' : 'ce6a862d-a4bb-4378-b14d-439806870c41',
    'id' : '4.4.5.2',
    'code' : '4.4.5.2',
    'abbr' : '4.4.5.2',
    'name' : { 'en': 'Unlucky' },
    'description' : 'Use this domain for words related to being unlucky.',
    'value' : '4.4.5.2 Unlucky'
  },
  {
    'guid' : 'aad7c9b0-aff3-4684-86d5-657a20e39288',
    'id' : '4.5',
    'code' : '4.5',
    'abbr' : '4.5',
    'name' : { 'en': 'Authority' },
    'description' : 'Use this domain for words related to authority.',
    'value' : '4.5 Authority'
  },
  {
    'guid' : '67087306-0211-4c28-b17e-0b6827723f07',
    'id' : '4.5.1',
    'code' : '4.5.1',
    'abbr' : '4.5.1',
    'name' : { 'en': 'Person in authority' },
    'description' : 'Use this domain for words related to a person in authority.',
    'value' : '4.5.1 Person in authority'
  },
  {
    'guid' : 'b8f59ddd-48de-4b3d-af47-687c9237ccd9',
    'id' : '4.5.2',
    'code' : '4.5.2',
    'abbr' : '4.5.2',
    'name' : { 'en': 'Have authority' },
    'description' : 'Use this domain for words related to having authority.',
    'value' : '4.5.2 Have authority'
  },
  {
    'guid' : '577017b0-ae87-4fa2-a51b-4f430497be75',
    'id' : '4.5.3',
    'code' : '4.5.3',
    'abbr' : '4.5.3',
    'name' : { 'en': 'Exercise authority' },
    'description' : 'Use this domain for words related to exercising authority.',
    'value' : '4.5.3 Exercise authority'
  },
  {
    'guid' : 'f70907c6-a064-425a-830f-e669319c38da',
    'id' : '4.5.3.1',
    'code' : '4.5.3.1',
    'abbr' : '4.5.3.1',
    'name' : { 'en': 'Lead' },
    'description' : 'Use this domain for words referring to one person leading or controlling other people because they have authority over them.',
    'value' : '4.5.3.1 Lead'
  },
  {
    'guid' : '94f919c2-bf8b-4ae5-a611-8c0cd4d7a5d2',
    'id' : '4.5.3.2',
    'code' : '4.5.3.2',
    'abbr' : '4.5.3.2',
    'name' : { 'en': 'Command' },
    'description' : 'Use this domain for words related to commanding someone to do something.',
    'value' : '4.5.3.2 Command'
  },
  {
    'guid' : 'a84a5ffc-006c-4d29-b71f-5215cc40a5a8',
    'id' : '4.5.3.3',
    'code' : '4.5.3.3',
    'abbr' : '4.5.3.3',
    'name' : { 'en': 'Discipline, train' },
    'description' : 'Use this domain for words related to disciplining someone.',
    'value' : '4.5.3.3 Discipline, train'
  },
  {
    'guid' : 'bf0bdeeb-564d-407b-8bdf-31221aff7364',
    'id' : '4.5.3.4',
    'code' : '4.5.3.4',
    'abbr' : '4.5.3.4',
    'name' : { 'en': 'Appoint, delegate' },
    'description' : 'Use this domain for words related to appointing someone to a position of authority, or delegating authority to someone.',
    'value' : '4.5.3.4 Appoint, delegate'
  },
  {
    'guid' : 'b0a9a631-e0dc-47d4-a762-e9a627732218',
    'id' : '4.5.4',
    'code' : '4.5.4',
    'abbr' : '4.5.4',
    'name' : { 'en': 'Submit to authority' },
    'description' : 'Use this domain for words related to submitting to authority.',
    'value' : '4.5.4 Submit to authority'
  },
  {
    'guid' : 'b632b00b-b03f-4549-8e02-6402c05a4f06',
    'id' : '4.5.4.1',
    'code' : '4.5.4.1',
    'abbr' : '4.5.4.1',
    'name' : { 'en': 'Obey' },
    'description' : 'Use this domain for words related to obeying someone.',
    'value' : '4.5.4.1 Obey'
  },
  {
    'guid' : '28ba8f5c-5baa-4500-a6f5-be292caa673f',
    'id' : '4.5.4.2',
    'code' : '4.5.4.2',
    'abbr' : '4.5.4.2',
    'name' : { 'en': 'Disobey' },
    'description' : 'Use this domain for words related to disobeying someone.',
    'value' : '4.5.4.2 Disobey'
  },
  {
    'guid' : '430ce279-1464-4d55-8483-5525a3c3094d',
    'id' : '4.5.4.3',
    'code' : '4.5.4.3',
    'abbr' : '4.5.4.3',
    'name' : { 'en': 'Serve' },
    'description' : 'Use this domain for words related to serving someone.',
    'value' : '4.5.4.3 Serve'
  },
  {
    'guid' : 'e1ac83c2-352f-4a2e-9612-99e66d6d3d0c',
    'id' : '4.5.4.4',
    'code' : '4.5.4.4',
    'abbr' : '4.5.4.4',
    'name' : { 'en': 'Slave' },
    'description' : 'Use this domain for words referring to a slave--a person owned by another and obligated to work without wages.',
    'value' : '4.5.4.4 Slave'
  },
  {
    'guid' : '896d57d4-4c25-4f0b-9985-a30974f64704',
    'id' : '4.5.4.5',
    'code' : '4.5.4.5',
    'abbr' : '4.5.4.5',
    'name' : { 'en': 'Follow, be a disciple' },
    'description' : 'Use this domain for words related to being a disciple of someone.',
    'value' : '4.5.4.5 Follow, be a disciple'
  },
  {
    'guid' : 'c60cf6a1-7868-4536-ac73-387bfa26e04b',
    'id' : '4.5.4.6',
    'code' : '4.5.4.6',
    'abbr' : '4.5.4.6',
    'name' : { 'en': 'Rebel against authority' },
    'description' : 'Use this domain for words related to rebelling against authority.',
    'value' : '4.5.4.6 Rebel against authority'
  },
  {
    'guid' : 'e9947962-a243-4a44-a94d-64d68718d88c',
    'id' : '4.5.4.7',
    'code' : '4.5.4.7',
    'abbr' : '4.5.4.7',
    'name' : { 'en': 'Independent' },
    'description' : 'Use this domain for words related to being independent.',
    'value' : '4.5.4.7 Independent'
  },
  {
    'guid' : '4a8c6c2e-7a8f-4dd6-97d3-20a35d7d10e9',
    'id' : '4.5.5',
    'code' : '4.5.5',
    'abbr' : '4.5.5',
    'name' : { 'en': 'Honor' },
    'description' : 'Use this domain for words related to honoring someone.',
    'value' : '4.5.5 Honor'
  },
  {
    'guid' : 'bc61bd8d-295b-4965-a183-703d21a56996',
    'id' : '4.5.5.1',
    'code' : '4.5.5.1',
    'abbr' : '4.5.5.1',
    'name' : { 'en': 'Title, name of honor' },
    'description' : 'Use this domain for words related to a title of honor.',
    'value' : '4.5.5.1 Title, name of honor'
  },
  {
    'guid' : 'bd9bb361-3c12-4c70-8098-40b0df9824ce',
    'id' : '4.5.5.2',
    'code' : '4.5.5.2',
    'abbr' : '4.5.5.2',
    'name' : { 'en': 'Lack respect' },
    'description' : 'Use this domain for words related to lacking respect.',
    'value' : '4.5.5.2 Lack respect'
  },
  {
    'guid' : '785e7f1f-21ed-46b6-8599-c6ced4fd26d8',
    'id' : '4.5.6',
    'code' : '4.5.6',
    'abbr' : '4.5.6',
    'name' : { 'en': 'Status' },
    'description' : 'Use this domain for words related to a person"s social status.',
    'value' : '4.5.6 Status'
  },
  {
    'guid' : 'a977c4b6-1004-448f-b6b0-daf5ce87d76d',
    'id' : '4.5.6.1',
    'code' : '4.5.6.1',
    'abbr' : '4.5.6.1',
    'name' : { 'en': 'High status' },
    'description' : 'Use this domain for words related to high social status.',
    'value' : '4.5.6.1 High status'
  },
  {
    'guid' : '9ed42115-8532-4f99-b0c0-36abfe9db652',
    'id' : '4.5.6.2',
    'code' : '4.5.6.2',
    'abbr' : '4.5.6.2',
    'name' : { 'en': 'Low status' },
    'description' : 'Use this domain for words related to low social status.',
    'value' : '4.5.6.2 Low status'
  },
  {
    'guid' : '7558d071-a885-447b-9aa2-604c29669492',
    'id' : '4.6',
    'code' : '4.6',
    'abbr' : '4.6',
    'name' : { 'en': 'Government' },
    'description' : 'Use this domain for words related to government.',
    'value' : '4.6 Government'
  },
  {
    'guid' : '7ddbd56f-e458-4a72-a9bc-9b7db6bde75a',
    'id' : '4.6.1',
    'code' : '4.6.1',
    'abbr' : '4.6.1',
    'name' : { 'en': 'Ruler' },
    'description' : 'Use this domain for words related to the ruler of a country.',
    'value' : '4.6.1 Ruler'
  },
  {
    'guid' : 'ded0e58d-8ad7-4909-b0f1-b9ab9c10bb0d',
    'id' : '4.6.1.1',
    'code' : '4.6.1.1',
    'abbr' : '4.6.1.1',
    'name' : { 'en': 'King"s family' },
    'description' : 'Use this domain for words related to the king"s family.',
    'value' : '4.6.1.1 King"s family'
  },
  {
    'guid' : '5f3e44b8-e94c-49b4-9e2d-1acd93dddf75',
    'id' : '4.6.1.2',
    'code' : '4.6.1.2',
    'abbr' : '4.6.1.2',
    'name' : { 'en': 'Government official' },
    'description' : 'Use this domain for words related to a government official.',
    'value' : '4.6.1.2 Government official'
  },
  {
    'guid' : '134c68a9-ac3f-4b7e-8fca-63642d796a75',
    'id' : '4.6.2',
    'code' : '4.6.2',
    'abbr' : '4.6.2',
    'name' : { 'en': 'Citizen' },
    'description' : 'Use this domain for words related to a citizen of a country.',
    'value' : '4.6.2 Citizen'
  },
  {
    'guid' : 'd2f516f4-df1c-44f6-8704-76dd52201317',
    'id' : '4.6.2.1',
    'code' : '4.6.2.1',
    'abbr' : '4.6.2.1',
    'name' : { 'en': 'Foreigner' },
    'description' : 'Use this domain for words related to a foreigner--a person who visits or lives in a country but is not a citizen.',
    'value' : '4.6.2.1 Foreigner'
  },
  {
    'guid' : '562f55de-efc7-41a7-b450-6f9dea2813e2',
    'id' : '4.6.3',
    'code' : '4.6.3',
    'abbr' : '4.6.3',
    'name' : { 'en': 'Government organization' },
    'description' : 'Use this domain for words related to a government organization.',
    'value' : '4.6.3 Government organization'
  },
  {
    'guid' : 'f9935962-9a14-485d-9bef-bd4a52dd92c1',
    'id' : '4.6.3.1',
    'code' : '4.6.3.1',
    'abbr' : '4.6.3.1',
    'name' : { 'en': 'Governing body' },
    'description' : 'Use this domain for words related to a governing body--an organization within the government.',
    'value' : '4.6.3.1 Governing body'
  },
  {
    'guid' : 'ff0a16f2-c44d-4ed4-9520-c214acfb68e5',
    'id' : '4.6.4',
    'code' : '4.6.4',
    'abbr' : '4.6.4',
    'name' : { 'en': 'Rule' },
    'description' : 'Use this domain for words related to ruling.',
    'value' : '4.6.4 Rule'
  },
  {
    'guid' : 'bf007cd9-925d-4073-a1d6-16d64a45ca25',
    'id' : '4.6.5',
    'code' : '4.6.5',
    'abbr' : '4.6.5',
    'name' : { 'en': 'Subjugate' },
    'description' : 'Use this domain for words related to a person subjugating someone to their authority.',
    'value' : '4.6.5 Subjugate'
  },
  {
    'guid' : '5e7a5899-df78-4aa7-bec9-b354acfe087f',
    'id' : '4.6.6',
    'code' : '4.6.6',
    'abbr' : '4.6.6',
    'name' : { 'en': 'Government functions' },
    'description' : 'Use this domain for words related to government functions--the things a government does.',
    'value' : '4.6.6 Government functions'
  },
  {
    'guid' : '16d2c60a-52d7-4ec5-a5b1-c559dc078bf3',
    'id' : '4.6.6.1',
    'code' : '4.6.6.1',
    'abbr' : '4.6.6.1',
    'name' : { 'en': 'Police' },
    'description' : 'Use this domain for words related to the police.',
    'value' : '4.6.6.1 Police'
  },
  {
    'guid' : '0448c78b-dbb7-417c-afc5-b227a1475825',
    'id' : '4.6.6.1.1',
    'code' : '4.6.6.1.1',
    'abbr' : '4.6.6.1.1',
    'name' : { 'en': 'Arrest' },
    'description' : 'Use this domain for words related to arresting a criminal.',
    'value' : '4.6.6.1.1 Arrest'
  },
  {
    'guid' : 'd7349bac-efc0-41ba-ba60-f23d38e97a36',
    'id' : '4.6.6.1.2',
    'code' : '4.6.6.1.2',
    'abbr' : '4.6.6.1.2',
    'name' : { 'en': 'Informal justice' },
    'description' : 'Use this domain for words related to informal justice--punishing someone when you are not in a position of authority, as when a mob catches and kills a criminal.',
    'value' : '4.6.6.1.2 Informal justice'
  },
  {
    'guid' : '8b6aecfb-071d-439d-9ee0-efa3c57967a0',
    'id' : '4.6.6.2',
    'code' : '4.6.6.2',
    'abbr' : '4.6.6.2',
    'name' : { 'en': 'Diplomacy' },
    'description' : 'Use this domain for words related to diplomacy between nations.',
    'value' : '4.6.6.2 Diplomacy'
  },
  {
    'guid' : '57ed66ee-f82b-4e80-955f-7492d85372b0',
    'id' : '4.6.6.3',
    'code' : '4.6.6.3',
    'abbr' : '4.6.6.3',
    'name' : { 'en': 'Represent' },
    'description' : 'Use this domain for words related to representing another person.',
    'value' : '4.6.6.3 Represent'
  },
  {
    'guid' : 'b53deac1-26c7-4fe9-9109-8496e248e8c7',
    'id' : '4.6.6.4',
    'code' : '4.6.6.4',
    'abbr' : '4.6.6.4',
    'name' : { 'en': 'Election' },
    'description' : 'Use this domain for words related to an election.',
    'value' : '4.6.6.4 Election'
  },
  {
    'guid' : 'd8631167-08bd-4571-bc7c-57a4407da51c',
    'id' : '4.6.6.5',
    'code' : '4.6.6.5',
    'abbr' : '4.6.6.5',
    'name' : { 'en': 'Politics' },
    'description' : 'Use this domain for words related to politics--the activities of politicians and political parties.',
    'value' : '4.6.6.5 Politics'
  },
  {
    'guid' : 'c8d94c8f-db0b-4016-bdd2-e41a2eae4288',
    'id' : '4.6.7',
    'code' : '4.6.7',
    'abbr' : '4.6.7',
    'name' : { 'en': 'Region' },
    'description' : 'Use this domain for words referring to a region--a part of a country, or a part of the earth.',
    'value' : '4.6.7 Region'
  },
  {
    'guid' : 'a7dae83a-dce6-47ea-ab3f-ac8a3af4cce9',
    'id' : '4.6.7.1',
    'code' : '4.6.7.1',
    'abbr' : '4.6.7.1',
    'name' : { 'en': 'Country' },
    'description' : 'Use this domain for words related to a country.',
    'value' : '4.6.7.1 Country'
  },
  {
    'guid' : 'b536622c-80a3-4b31-9d22-4ed2fb76324d',
    'id' : '4.6.7.2',
    'code' : '4.6.7.2',
    'abbr' : '4.6.7.2',
    'name' : { 'en': 'City' },
    'description' : 'Use this domain for words related to a city.',
    'value' : '4.6.7.2 City'
  },
  {
    'guid' : '0f983449-1c43-4974-b388-7695b1af4bfa',
    'id' : '4.6.7.3',
    'code' : '4.6.7.3',
    'abbr' : '4.6.7.3',
    'name' : { 'en': 'Countryside' },
    'description' : 'Use this domain for words related to the countryside--the area away from a city',
    'value' : '4.6.7.3 Countryside'
  },
  {
    'guid' : 'ec998dc6-d509-4832-8434-d2abac34ba70',
    'id' : '4.6.7.4',
    'code' : '4.6.7.4',
    'abbr' : '4.6.7.4',
    'name' : { 'en': 'Community' },
    'description' : 'Use this domain for words related to a community.',
    'value' : '4.6.7.4 Community'
  },
  {
    'guid' : '87bd0d45-8a94-41cb-9864-90d53a11a4b9',
    'id' : '4.7',
    'code' : '4.7',
    'abbr' : '4.7',
    'name' : { 'en': 'Law' },
    'description' : 'Use this domain for words related to the law.',
    'value' : '4.7 Law'
  },
  {
    'guid' : 'f51bcafa-e624-4555-b8f1-b5726d74734d',
    'id' : '4.7.1',
    'code' : '4.7.1',
    'abbr' : '4.7.1',
    'name' : { 'en': 'Laws' },
    'description' : 'Use this domain for words referring to a specific law or set of laws.',
    'value' : '4.7.1 Laws'
  },
  {
    'guid' : '05a7bbdc-7cf5-47d3-830b-84e1591b11cc',
    'id' : '4.7.2',
    'code' : '4.7.2',
    'abbr' : '4.7.2',
    'name' : { 'en': 'Pass laws' },
    'description' : 'Use this domain for words related to passing a law.',
    'value' : '4.7.2 Pass laws'
  },
  {
    'guid' : '1dc717b9-c5e8-4482-b076-22102da9d553',
    'id' : '4.7.3',
    'code' : '4.7.3',
    'abbr' : '4.7.3',
    'name' : { 'en': 'Break the law' },
    'description' : 'Use this domain for words related to breaking the law.',
    'value' : '4.7.3 Break the law'
  },
  {
    'guid' : '30ea3057-753d-4c4c-9b1f-ed30e569feea',
    'id' : '4.7.4',
    'code' : '4.7.4',
    'abbr' : '4.7.4',
    'name' : { 'en': 'Court of law' },
    'description' : 'Use this domain for words related to a court of law.',
    'value' : '4.7.4 Court of law'
  },
  {
    'guid' : 'cca44b46-437c-42ee-93d7-a8820d61d0c8',
    'id' : '4.7.4.1',
    'code' : '4.7.4.1',
    'abbr' : '4.7.4.1',
    'name' : { 'en': 'Legal personnel' },
    'description' : 'Use this domain for words related to legal personnel.',
    'value' : '4.7.4.1 Legal personnel'
  },
  {
    'guid' : '85214614-ab45-4805-9014-092750d47511',
    'id' : '4.7.5',
    'code' : '4.7.5',
    'abbr' : '4.7.5',
    'name' : { 'en': 'Trial' },
    'description' : 'Use this domain for words related to the legal process.',
    'value' : '4.7.5 Trial'
  },
  {
    'guid' : 'e10b9449-08a3-4c13-aff2-31486749b62f',
    'id' : '4.7.5.1',
    'code' : '4.7.5.1',
    'abbr' : '4.7.5.1',
    'name' : { 'en': 'Investigate a crime' },
    'description' : 'Use this domain for words referring to investigating a crime, accident, or criminal--to try to learn something about something bad that has happened because you want to know who did it, or to try to learn something about someone because you think they did something bad.',
    'value' : '4.7.5.1 Investigate a crime'
  },
  {
    'guid' : '67d74de1-33f9-4d39-8fa5-5dd27e7cd1d1',
    'id' : '4.7.5.2',
    'code' : '4.7.5.2',
    'abbr' : '4.7.5.2',
    'name' : { 'en': 'Suspect' },
    'description' : 'Use this domain for words related to suspecting someone--to think that someone might have done something bad.',
    'value' : '4.7.5.2 Suspect'
  },
  {
    'guid' : 'fa660c9d-8787-4335-8744-3dbc139b2df1',
    'id' : '4.7.5.3',
    'code' : '4.7.5.3',
    'abbr' : '4.7.5.3',
    'name' : { 'en': 'Accuse, confront' },
    'description' : 'Use this domain for words related to accusing someone of doing something bad.',
    'value' : '4.7.5.3 Accuse, confront'
  },
  {
    'guid' : 'e83586c6-8d8e-4a23-bdda-a1731a5ece22',
    'id' : '4.7.5.4',
    'code' : '4.7.5.4',
    'abbr' : '4.7.5.4',
    'name' : { 'en': 'Defend against accusation' },
    'description' : 'Use this domain for words related to defending someone who has been accused of breaking a law.',
    'value' : '4.7.5.4 Defend against accusation'
  },
  {
    'guid' : 'ca511a0c-5628-4726-8a6e-aa9fa3b73bfc',
    'id' : '4.7.5.5',
    'code' : '4.7.5.5',
    'abbr' : '4.7.5.5',
    'name' : { 'en': 'Witness, testify' },
    'description' : 'Use this domain for words related to testifying in a court of law.',
    'value' : '4.7.5.5 Witness, testify'
  },
  {
    'guid' : '70963c34-dd34-40c2-bb21-e9cea73c7923',
    'id' : '4.7.5.6',
    'code' : '4.7.5.6',
    'abbr' : '4.7.5.6',
    'name' : { 'en': 'Drop charges' },
    'description' : 'Use this domain for words related to dropping legal charges against someone.',
    'value' : '4.7.5.6 Drop charges'
  },
  {
    'guid' : '59d19623-0f3b-484d-96eb-a9093b020c8d',
    'id' : '4.7.5.7',
    'code' : '4.7.5.7',
    'abbr' : '4.7.5.7',
    'name' : { 'en': 'Take oath' },
    'description' : 'Use this domain for words related to taking an oath.',
    'value' : '4.7.5.7 Take oath'
  },
  {
    'guid' : '2cd48908-8f12-4e0f-a22e-87237618ce9f',
    'id' : '4.7.5.8',
    'code' : '4.7.5.8',
    'abbr' : '4.7.5.8',
    'name' : { 'en': 'Vindicate' },
    'description' : 'Use this domain for words referring to vindicating someone--to prove that someone is innocent of an accusation made against them.',
    'value' : '4.7.5.8 Vindicate'
  },
  {
    'guid' : 'bd7d0c9c-791e-4c34-b9ed-ebddad8f9724',
    'id' : '4.7.6',
    'code' : '4.7.6',
    'abbr' : '4.7.6',
    'name' : { 'en': 'Judge, render a verdict' },
    'description' : 'Use this domain for words related to judging someone.',
    'value' : '4.7.6 Judge, render a verdict'
  },
  {
    'guid' : '0c1bcc98-9bc3-4da0-8eac-ff8a6eecbf84',
    'id' : '4.7.6.1',
    'code' : '4.7.6.1',
    'abbr' : '4.7.6.1',
    'name' : { 'en': 'Acquit' },
    'description' : 'Use this domain for words related to acquitting a person.',
    'value' : '4.7.6.1 Acquit'
  },
  {
    'guid' : '0ede51d2-69bd-411e-97f9-da0d5118bbff',
    'id' : '4.7.6.2',
    'code' : '4.7.6.2',
    'abbr' : '4.7.6.2',
    'name' : { 'en': 'Condemn, find guilty' },
    'description' : 'Use this domain for words related to condemning someone.',
    'value' : '4.7.6.2 Condemn, find guilty'
  },
  {
    'guid' : '8570f05c-a152-4117-9f54-4edfa9c06a32',
    'id' : '4.7.6.3',
    'code' : '4.7.6.3',
    'abbr' : '4.7.6.3',
    'name' : { 'en': 'Fault' },
    'description' : 'Use this domain for words related to something being someone"s fault.',
    'value' : '4.7.6.3 Fault'
  },
  {
    'guid' : '6bf7569e-dc79-49da-9ce1-e2e03303828a',
    'id' : '4.7.7',
    'code' : '4.7.7',
    'abbr' : '4.7.7',
    'name' : { 'en': 'Punish' },
    'description' : 'Use this domain for words related to punishing someone.',
    'value' : '4.7.7 Punish'
  },
  {
    'guid' : 'ba98f891-77df-4910-8657-38f4ba79d3a5',
    'id' : '4.7.7.1',
    'code' : '4.7.7.1',
    'abbr' : '4.7.7.1',
    'name' : { 'en': 'Reward' },
    'description' : 'Use this domain for words related to rewarding someone.',
    'value' : '4.7.7.1 Reward'
  },
  {
    'guid' : 'feca6b23-1ca1-4d99-ac79-3672d1d1f7db',
    'id' : '4.7.7.2',
    'code' : '4.7.7.2',
    'abbr' : '4.7.7.2',
    'name' : { 'en': 'Fine' },
    'description' : 'Use this domain for words referring to a fine--a payment (usually of money) made to a victim or the government for a crime committed against them.',
    'value' : '4.7.7.2 Fine'
  },
  {
    'guid' : '13f62fa1-589c-4a46-9bbc-b0fd1001e21f',
    'id' : '4.7.7.3',
    'code' : '4.7.7.3',
    'abbr' : '4.7.7.3',
    'name' : { 'en': 'Imprison' },
    'description' : 'Use this domain for words related to imprisoning someone.',
    'value' : '4.7.7.3 Imprison'
  },
  {
    'guid' : 'bf5175f6-fbe4-4ac6-9041-f8aa78b7ac78',
    'id' : '4.7.7.4',
    'code' : '4.7.7.4',
    'abbr' : '4.7.7.4',
    'name' : { 'en': 'Execute' },
    'description' : 'Use this domain for words related to executing someone for a crime.',
    'value' : '4.7.7.4 Execute'
  },
  {
    'guid' : '55a7b809-4196-4c5a-a6d6-09b586ce71e7',
    'id' : '4.7.7.5',
    'code' : '4.7.7.5',
    'abbr' : '4.7.7.5',
    'name' : { 'en': 'Ostracize' },
    'description' : 'Use this domain for words referring to ostracizing someone--excluding someone from society as punishment for some wrong.',
    'value' : '4.7.7.5 Ostracize'
  },
  {
    'guid' : '9fcae400-ce8f-4e30-9516-97ab794c30a9',
    'id' : '4.7.7.6',
    'code' : '4.7.7.6',
    'abbr' : '4.7.7.6',
    'name' : { 'en': 'Pardon, release' },
    'description' : 'Use this domain for words related to pardoning someone who has been found guilty of a crime.',
    'value' : '4.7.7.6 Pardon, release'
  },
  {
    'guid' : '62efa729-0920-4933-93f3-b6a48519a5c7',
    'id' : '4.7.7.7',
    'code' : '4.7.7.7',
    'abbr' : '4.7.7.7',
    'name' : { 'en': 'Atone, restitution' },
    'description' : 'Use this domain for words referring to atoning for a past sin--doing something to make up for something bad you did, or giving something to pay for  something bad you did.',
    'value' : '4.7.7.7 Atone, restitution'
  },
  {
    'guid' : 'ef6d136e-ac1d-48b9-819d-252485534557',
    'id' : '4.7.8',
    'code' : '4.7.8',
    'abbr' : '4.7.8',
    'name' : { 'en': 'Legal contract' },
    'description' : 'Use this domain for words related to a legal contract.',
    'value' : '4.7.8 Legal contract'
  },
  {
    'guid' : '69455770-fca7-4f41-9e7d-236e8f094ce6',
    'id' : '4.7.8.1',
    'code' : '4.7.8.1',
    'abbr' : '4.7.8.1',
    'name' : { 'en': 'Covenant' },
    'description' : 'Use this domain for words related to a covenant between two people or groups of people.',
    'value' : '4.7.8.1 Covenant'
  },
  {
    'guid' : '6aa8133a-2578-4617-bd9c-6428e897a4f1',
    'id' : '4.7.8.2',
    'code' : '4.7.8.2',
    'abbr' : '4.7.8.2',
    'name' : { 'en': 'Break a contract' },
    'description' : 'Use this domain for words related to breaking a contract.',
    'value' : '4.7.8.2 Break a contract'
  },
  {
    'guid' : '596ab399-e442-4afe-8796-633a49de65a7',
    'id' : '4.7.9',
    'code' : '4.7.9',
    'abbr' : '4.7.9',
    'name' : { 'en': 'Justice' },
    'description' : 'Use this domain for words related to justice.',
    'value' : '4.7.9 Justice'
  },
  {
    'guid' : '38bdff04-c7a9-41fa-a6a2-7aa214de308c',
    'id' : '4.7.9.1',
    'code' : '4.7.9.1',
    'abbr' : '4.7.9.1',
    'name' : { 'en': 'Impartial' },
    'description' : 'Use this domain for words related to being impartial.',
    'value' : '4.7.9.1 Impartial'
  },
  {
    'guid' : '793d3124-8b77-4ff0-82ec-09a3a9d8d865',
    'id' : '4.7.9.2',
    'code' : '4.7.9.2',
    'abbr' : '4.7.9.2',
    'name' : { 'en': 'Unfair' },
    'description' : 'Use this domain for words related to being unfair.',
    'value' : '4.7.9.2 Unfair'
  },
  {
    'guid' : '5737714d-49e4-4eb4-8e46-03203ee5340b',
    'id' : '4.7.9.3',
    'code' : '4.7.9.3',
    'abbr' : '4.7.9.3',
    'name' : { 'en': 'Deserve' },
    'description' : 'Use this domain for words related to deserving something--if you do something, either good or bad, something can happen to you as a result of what you did. For instance you can receive a reward for doing good, or a punishment for doing wrong. If what happened to you is like what you did, people can think it is right that this thing happened to you.',
    'value' : '4.7.9.3 Deserve'
  },
  {
    'guid' : 'f9516c66-ac2c-49dd-951a-0d3606450463',
    'id' : '4.7.9.4',
    'code' : '4.7.9.4',
    'abbr' : '4.7.9.4',
    'name' : { 'en': 'Discriminate, be unfair' },
    'description' : 'Use this domain for words related to being unfair to someone.',
    'value' : '4.7.9.4 Discriminate, be unfair'
  },
  {
    'guid' : 'bd31529c-ab67-419b-89a4-949aee8b3b11',
    'id' : '4.7.9.5',
    'code' : '4.7.9.5',
    'abbr' : '4.7.9.5',
    'name' : { 'en': 'Act harshly' },
    'description' : 'Use this domain for words related to acting harshly.',
    'value' : '4.7.9.5 Act harshly'
  },
  {
    'guid' : 'bb4e0a69-db20-4757-beff-5dcb1c5e0f92',
    'id' : '4.7.9.6',
    'code' : '4.7.9.6',
    'abbr' : '4.7.9.6',
    'name' : { 'en': 'Oppress' },
    'description' : 'Use this domain for words related to oppressing someone or a particular group of people--when a person uses his power or authority to harm others who are innocent.',
    'value' : '4.7.9.6 Oppress'
  },
  {
    'guid' : 'f7da1907-e6c5-4d21-a8e8-81376f3467df',
    'id' : '4.8',
    'code' : '4.8',
    'abbr' : '4.8',
    'name' : { 'en': 'Conflict' },
    'description' : 'Use this domain for general words referring to conflict between people, including quarreling, fighting, and war. The words in this domain should be very general, rather than referring to specific kinds of conflict.',
    'value' : '4.8 Conflict'
  },
  {
    'guid' : '189f8c29-f0ff-44b6-a0db-5b287c412a75',
    'id' : '4.8.1',
    'code' : '4.8.1',
    'abbr' : '4.8.1',
    'name' : { 'en': 'Hostility' },
    'description' : 'The words in this domain describe a situation in which people disagree about something so strongly that they might start fighting. But these words imply that they have not started fighting yet.',
    'value' : '4.8.1 Hostility'
  },
  {
    'guid' : '763fa2e0-c119-4f50-a307-81ed8c3497ed',
    'id' : '4.8.1.1',
    'code' : '4.8.1.1',
    'abbr' : '4.8.1.1',
    'name' : { 'en': 'Oppose' },
    'description' : 'Use this domain for words referring to opposing something that you think is wrong.',
    'value' : '4.8.1.1 Oppose'
  },
  {
    'guid' : '55201761-fe2e-40d5-a2a7-8079e00a2c32',
    'id' : '4.8.2',
    'code' : '4.8.2',
    'abbr' : '4.8.2',
    'name' : { 'en': 'Fight' },
    'description' : 'Use this domain for words referring to fighting someone. The words in this domain describe a situation in which two people or groups of people fight each other over something or in order to reach some goal. A fight can use words, various kinds of weapons, or other actions. For fights that only use words use the domain\n"Quarrel".',
    'value' : '4.8.2 Fight'
  },
  {
    'guid' : 'e072bd42-eb0f-48c8-97fd-ae9ca8bc3a75',
    'id' : '4.8.2.1',
    'code' : '4.8.2.1',
    'abbr' : '4.8.2.1',
    'name' : { 'en': 'Fight for something good' },
    'description' : 'Use this domain for words referring to fighting for something good.',
    'value' : '4.8.2.1 Fight for something good'
  },
  {
    'guid' : '37e6c8b5-f63c-4f5b-8c16-eccd727d6618',
    'id' : '4.8.2.2',
    'code' : '4.8.2.2',
    'abbr' : '4.8.2.2',
    'name' : { 'en': 'Fight against something bad' },
    'description' : 'Use this domain for words referring to fighting against something bad.',
    'value' : '4.8.2.2 Fight against something bad'
  },
  {
    'guid' : '80563285-4de7-4040-8080-a5b22208e7d5',
    'id' : '4.8.2.3',
    'code' : '4.8.2.3',
    'abbr' : '4.8.2.3',
    'name' : { 'en': 'Attack' },
    'description' : 'Use this domain for words referring to attacking someone--to begin fighting someone.',
    'value' : '4.8.2.3 Attack'
  },
  {
    'guid' : '5489f4ae-34a7-4f8b-9086-4247b0d8b3de',
    'id' : '4.8.2.3.1',
    'code' : '4.8.2.3.1',
    'abbr' : '4.8.2.3.1',
    'name' : { 'en': 'Ambush' },
    'description' : 'Use this domain for words related to ambushing someone--to attacking someone without warning.',
    'value' : '4.8.2.3.1 Ambush'
  },
  {
    'guid' : '243d8a57-d5ed-4d7f-bd5e-f2605634f0fc',
    'id' : '4.8.2.4',
    'code' : '4.8.2.4',
    'abbr' : '4.8.2.4',
    'name' : { 'en': 'Defend' },
    'description' : 'Use this domain for words related to defending someone from attack.',
    'value' : '4.8.2.4 Defend'
  },
  {
    'guid' : '530ff7e0-e3cb-4dc3-9c1c-3034969e1ce8',
    'id' : '4.8.2.5',
    'code' : '4.8.2.5',
    'abbr' : '4.8.2.5',
    'name' : { 'en': 'Revenge' },
    'description' : 'Use this domain for words related to revenge--to do something bad to someone because they did something bad to you.',
    'value' : '4.8.2.5 Revenge'
  },
  {
    'guid' : '8594cb26-9f3c-48f5-b00b-f055eb5a5e61',
    'id' : '4.8.2.6',
    'code' : '4.8.2.6',
    'abbr' : '4.8.2.6',
    'name' : { 'en': 'Riot' },
    'description' : 'Use this domain for words related to a riot--when lots of people are fighting and breaking the law.',
    'value' : '4.8.2.6 Riot'
  },
  {
    'guid' : '0066f0f7-02dd-4f8e-afa5-59b8cb5a434a',
    'id' : '4.8.2.7',
    'code' : '4.8.2.7',
    'abbr' : '4.8.2.7',
    'name' : { 'en': 'Betray' },
    'description' : 'Use this domain for words related to betraying someone.',
    'value' : '4.8.2.7 Betray'
  },
  {
    'guid' : '8a7fdd75-01dc-4d40-84ff-fa0484da3abb',
    'id' : '4.8.2.8',
    'code' : '4.8.2.8',
    'abbr' : '4.8.2.8',
    'name' : { 'en': 'Violent' },
    'description' : 'Use this domain for words related to being violent--a word describing someone who is likely to attack and injure or kill people.',
    'value' : '4.8.2.8 Violent'
  },
  {
    'guid' : '77f27500-aad8-409c-a28e-92df73794dce',
    'id' : '4.8.2.9',
    'code' : '4.8.2.9',
    'abbr' : '4.8.2.9',
    'name' : { 'en': 'Enemy' },
    'description' : 'Use this domain for words related to an enemy--someone you are fighting against.',
    'value' : '4.8.2.9 Enemy'
  },
  {
    'guid' : 'a3ba10f3-66e3-4ad5-8057-453b8941e497',
    'id' : '4.8.3',
    'code' : '4.8.3',
    'abbr' : '4.8.3',
    'name' : { 'en': 'War' },
    'description' : 'Use this domain for words related to war--fighting between countries.',
    'value' : '4.8.3 War'
  },
  {
    'guid' : '225c48dd-9fc2-4467-944f-16a098b4e518',
    'id' : '4.8.3.1',
    'code' : '4.8.3.1',
    'abbr' : '4.8.3.1',
    'name' : { 'en': 'Defeat' },
    'description' : 'Use this domain for words related to defeating someone.',
    'value' : '4.8.3.1 Defeat'
  },
  {
    'guid' : 'c8595a5f-4dde-4260-b8d8-265d0554ce93',
    'id' : '4.8.3.2',
    'code' : '4.8.3.2',
    'abbr' : '4.8.3.2',
    'name' : { 'en': 'Win' },
    'description' : 'Use this domain for words related to winning a victory.',
    'value' : '4.8.3.2 Win'
  },
  {
    'guid' : '17102138-b97a-4f1d-81bc-9be4af90889e',
    'id' : '4.8.3.3',
    'code' : '4.8.3.3',
    'abbr' : '4.8.3.3',
    'name' : { 'en': 'Lose a fight' },
    'description' : 'Use this domain for words related to losing a fight.',
    'value' : '4.8.3.3 Lose a fight'
  },
  {
    'guid' : '8a5c87ad-2d15-40c2-9f15-d7942ac80261',
    'id' : '4.8.3.4',
    'code' : '4.8.3.4',
    'abbr' : '4.8.3.4',
    'name' : { 'en': 'Surrender' },
    'description' : 'Use this domain for words related to surrendering to an enemy.',
    'value' : '4.8.3.4 Surrender'
  },
  {
    'guid' : '21461d78-02f9-4be6-80e3-6a4498ce8f4c',
    'id' : '4.8.3.5',
    'code' : '4.8.3.5',
    'abbr' : '4.8.3.5',
    'name' : { 'en': 'Prisoner of war' },
    'description' : 'Use this domain for words related to a prisoner of war.',
    'value' : '4.8.3.5 Prisoner of war'
  },
  {
    'guid' : 'f595deab-1838-4ddb-9ebe-55fb3007b309',
    'id' : '4.8.3.6',
    'code' : '4.8.3.6',
    'abbr' : '4.8.3.6',
    'name' : { 'en': 'Military organization' },
    'description' : 'Use this domain for words related to military organizations.',
    'value' : '4.8.3.6 Military organization'
  },
  {
    'guid' : 'd90db6d4-6c78-4ac8-9764-0cafa79b8b31',
    'id' : '4.8.3.6.1',
    'code' : '4.8.3.6.1',
    'abbr' : '4.8.3.6.1',
    'name' : { 'en': 'Army' },
    'description' : 'Use this domain for words related to the army.',
    'value' : '4.8.3.6.1 Army'
  },
  {
    'guid' : '7d629c80-e5c2-409f-a592-39c56e9ace6d',
    'id' : '4.8.3.6.2',
    'code' : '4.8.3.6.2',
    'abbr' : '4.8.3.6.2',
    'name' : { 'en': 'Navy' },
    'description' : 'Use this domain for words related to the navy.',
    'value' : '4.8.3.6.2 Navy'
  },
  {
    'guid' : 'eb842dc1-ad9c-4c1a-9eb2-a48b7f3092be',
    'id' : '4.8.3.6.3',
    'code' : '4.8.3.6.3',
    'abbr' : '4.8.3.6.3',
    'name' : { 'en': 'Air force' },
    'description' : 'Use this domain for words related to the air force.',
    'value' : '4.8.3.6.3 Air force'
  },
  {
    'guid' : 'f5e2ad18-5ad4-4186-9572-b1542096759e',
    'id' : '4.8.3.6.4',
    'code' : '4.8.3.6.4',
    'abbr' : '4.8.3.6.4',
    'name' : { 'en': 'Soldier' },
    'description' : 'Use this domain for words related to a soldier.',
    'value' : '4.8.3.6.4 Soldier'
  },
  {
    'guid' : '362a2bdd-985e-4bc0-a41c-358bd1babb12',
    'id' : '4.8.3.6.5',
    'code' : '4.8.3.6.5',
    'abbr' : '4.8.3.6.5',
    'name' : { 'en': 'Spy' },
    'description' : 'Use this domain for words related to a spy.',
    'value' : '4.8.3.6.5 Spy'
  },
  {
    'guid' : '98830eda-3997-4f4a-9ba4-664df669e7e2',
    'id' : '4.8.3.6.6',
    'code' : '4.8.3.6.6',
    'abbr' : '4.8.3.6.6',
    'name' : { 'en': 'Fort' },
    'description' : 'Use this domain for words related to a fort.',
    'value' : '4.8.3.6.6 Fort'
  },
  {
    'guid' : '738a09a5-59df-40f9-8a4e-176e00d03bbf',
    'id' : '4.8.3.7',
    'code' : '4.8.3.7',
    'abbr' : '4.8.3.7',
    'name' : { 'en': 'Weapon, shoot' },
    'description' : 'Use this domain for words related to a weapon and using a weapon.',
    'value' : '4.8.3.7 Weapon, shoot'
  },
  {
    'guid' : '3615c3d1-fd5b-40c5-80ad-80bfd6451d56',
    'id' : '4.8.3.8',
    'code' : '4.8.3.8',
    'abbr' : '4.8.3.8',
    'name' : { 'en': 'Plunder' },
    'description' : 'Use this domain for words related to plundering--stealing something from an enemy during a war.',
    'value' : '4.8.3.8 Plunder'
  },
  {
    'guid' : '0b7b9a1c-588b-475a-ac14-00f0999cbfe9',
    'id' : '4.8.4',
    'code' : '4.8.4',
    'abbr' : '4.8.4',
    'name' : { 'en': 'Peace' },
    'description' : 'Use this domain for words related to peace--when people or countries are not fighting.',
    'value' : '4.8.4 Peace'
  },
  {
    'guid' : '6eafbb5a-26ba-44b5-a0d5-21b9e7750ece',
    'id' : '4.8.4.1',
    'code' : '4.8.4.1',
    'abbr' : '4.8.4.1',
    'name' : { 'en': 'Rebuke' },
    'description' : 'Use this domain for words related to rebuking someone--to tell someone that he has done something wrong.',
    'value' : '4.8.4.1 Rebuke'
  },
  {
    'guid' : '9afb64d6-feae-4490-9b7e-95768627f643',
    'id' : '4.8.4.2',
    'code' : '4.8.4.2',
    'abbr' : '4.8.4.2',
    'name' : { 'en': 'Make an appeal' },
    'description' : 'Use this domain for words related to making an appeal.',
    'value' : '4.8.4.2 Make an appeal'
  },
  {
    'guid' : 'fd7e03f8-61c9-47e9-afa4-ab8917db03a5',
    'id' : '4.8.4.3',
    'code' : '4.8.4.3',
    'abbr' : '4.8.4.3',
    'name' : { 'en': 'Appease' },
    'description' : 'Use this domain for words related to appeasing someone.',
    'value' : '4.8.4.3 Appease'
  },
  {
    'guid' : '675eccbb-9858-4cd4-8405-5f0d0faa792c',
    'id' : '4.8.4.4',
    'code' : '4.8.4.4',
    'abbr' : '4.8.4.4',
    'name' : { 'en': 'Negotiate' },
    'description' : 'Use this domain for words related to negotiating with someone.',
    'value' : '4.8.4.4 Negotiate'
  },
  {
    'guid' : '9b9ccd76-76d6-457b-93d2-c4d242a395f8',
    'id' : '4.8.4.5',
    'code' : '4.8.4.5',
    'abbr' : '4.8.4.5',
    'name' : { 'en': 'Renounce claim, concede' },
    'description' : 'Use this domain for words related to renouncing a claim.',
    'value' : '4.8.4.5 Renounce claim, concede'
  },
  {
    'guid' : '1ca26512-75f6-4a7a-a7cc-07d08aa799d9',
    'id' : '4.8.4.6',
    'code' : '4.8.4.6',
    'abbr' : '4.8.4.6',
    'name' : { 'en': 'Repent' },
    'description' : 'Use this domain for words related to repenting.',
    'value' : '4.8.4.6 Repent'
  },
  {
    'guid' : '4b669bed-ba46-41cc-bcba-c2ef8e129c85',
    'id' : '4.8.4.6.1',
    'code' : '4.8.4.6.1',
    'abbr' : '4.8.4.6.1',
    'name' : { 'en': 'Request forgiveness' },
    'description' : 'Use this domain for words related to asking for forgiveness.',
    'value' : '4.8.4.6.1 Request forgiveness'
  },
  {
    'guid' : '7d7c81d5-9713-423f-b12e-8e11e451f0a7',
    'id' : '4.8.4.7',
    'code' : '4.8.4.7',
    'abbr' : '4.8.4.7',
    'name' : { 'en': 'Forgive' },
    'description' : 'Use this domain for words related to forgiving someone.',
    'value' : '4.8.4.7 Forgive'
  },
  {
    'guid' : 'a1dd1d94-fa8e-4325-9f72-b39bcac69755',
    'id' : '4.8.4.8',
    'code' : '4.8.4.8',
    'abbr' : '4.8.4.8',
    'name' : { 'en': 'Make peace' },
    'description' : 'Use this domain for words related to making peace--to try to prevent or end a war.',
    'value' : '4.8.4.8 Make peace'
  },
  {
    'guid' : '9e2b0c61-304e-4cad-9708-792bfde880b4',
    'id' : '4.8.4.8.1',
    'code' : '4.8.4.8.1',
    'abbr' : '4.8.4.8.1',
    'name' : { 'en': 'Stop fighting' },
    'description' : 'Use this domain for words related to stopping fighting.',
    'value' : '4.8.4.8.1 Stop fighting'
  },
  {
    'guid' : '3f069313-4827-4fc5-b73b-b9fbd42ca38c',
    'id' : '4.8.4.9',
    'code' : '4.8.4.9',
    'abbr' : '4.8.4.9',
    'name' : { 'en': 'Reconcile' },
    'description' : 'Use this domain for words related to reconciling with someone.',
    'value' : '4.8.4.9 Reconcile'
  },
  {
    'guid' : '7556a257-3703-40d3-91d3-c891fb250947',
    'id' : '4.9',
    'code' : '4.9',
    'abbr' : '4.9',
    'name' : { 'en': 'Religion' },
    'description' : 'Use this domain for general words referring to religion and the supernatural.',
    'value' : '4.9 Religion'
  },
  {
    'guid' : 'b60bf544-7774-4623-8c67-19b32b53dea2',
    'id' : '4.9.1',
    'code' : '4.9.1',
    'abbr' : '4.9.1',
    'name' : { 'en': 'God' },
    'description' : 'Use this domain for words related to God--the supreme being in the universe. Each theological system will have different beliefs concerning the existence and nature of God. Our purpose here is to collect and define the terms used to refer to the supreme deity. If there is no such person in the theological system, then use this domain for other terms for the pantheon of gods, ultimate reality, nirvana, and similar concepts. However most theological systems, even atheism, have the concept of a supreme God and use words to refer to him.',
    'value' : '4.9.1 God'
  },
  {
    'guid' : '7f6c81fb-02a4-415f-a363-fb11cc6b9254',
    'id' : '4.9.2',
    'code' : '4.9.2',
    'abbr' : '4.9.2',
    'name' : { 'en': 'Supernatural being' },
    'description' : 'Use this domain for words referring to supernatural beings--gods, spirits, other types of beings, which normally cannot be seen and do not belong to this world. Some people accept the existence of certain supernatural beings and not others. Mythological beings are those that were believed in during previous times but that are no longer believed in. Fictional beings are those that no one has ever believed in. An indication of whether most people believe in the supernatural being should be put in the definition.',
    'value' : '4.9.2 Supernatural being'
  },
  {
    'guid' : '25bf6690-6ed1-42e9-8a4a-3518f9cf382c',
    'id' : '4.9.3',
    'code' : '4.9.3',
    'abbr' : '4.9.3',
    'name' : { 'en': 'Theology' },
    'description' : 'Use this domain for words related to theology--the study of God and what people believe about God.',
    'value' : '4.9.3 Theology'
  },
  {
    'guid' : 'e9fdc131-addb-4db6-8f79-ae0044e1eb81',
    'id' : '4.9.3.1',
    'code' : '4.9.3.1',
    'abbr' : '4.9.3.1',
    'name' : { 'en': 'Sacred writings' },
    'description' : 'Use this domain for words related to sacred writings. Examples are only given for the Christian sacred writings. However you should include words referring to the holy books of all religions .',
    'value' : '4.9.3.1 Sacred writings'
  },
  {
    'guid' : '61f52196-a8b2-496d-96ea-8c15dc7d377a',
    'id' : '4.9.4',
    'code' : '4.9.4',
    'abbr' : '4.9.4',
    'name' : { 'en': 'Miracle, supernatural power' },
    'description' : 'Use this domain for words related to miracles--the use of supernatural power to do something good.',
    'value' : '4.9.4 Miracle, supernatural power'
  },
  {
    'guid' : 'b6e45998-9f6a-4b19-9cda-62410a11afa2',
    'id' : '4.9.4.1',
    'code' : '4.9.4.1',
    'abbr' : '4.9.4.1',
    'name' : { 'en': 'Sorcery' },
    'description' : 'Use this domain for words related to sorcery--the use of supernatural power to do something bad.',
    'value' : '4.9.4.1 Sorcery'
  },
  {
    'guid' : '3ea52505-aa6c-4f28-b475-f15ac1820ec1',
    'id' : '4.9.4.2',
    'code' : '4.9.4.2',
    'abbr' : '4.9.4.2',
    'name' : { 'en': 'Demon possession' },
    'description' : 'Use this domain for words referring to demon possession--when a demon or spirit influences or controls the behavior of a person. Use this domain for all words related to the relationship between spirits and people, including communication between people and spirits. Also use this domain for words referring to casting out demons--causing a demon to stop influencing or controlling a person.',
    'value' : '4.9.4.2 Demon possession'
  },
  {
    'guid' : '38d1a6fe-0811-4eb0-a1d8-f69b6ad978e0',
    'id' : '4.9.4.3',
    'code' : '4.9.4.3',
    'abbr' : '4.9.4.3',
    'name' : { 'en': 'Bless' },
    'description' : 'Use this domain for words related to blessing someone--saying something that causes something good to happen, or requests God to do something good to someone.',
    'value' : '4.9.4.3 Bless'
  },
  {
    'guid' : '106c2c42-36fd-4b0a-94f7-e998f6eae6f5',
    'id' : '4.9.4.4',
    'code' : '4.9.4.4',
    'abbr' : '4.9.4.4',
    'name' : { 'en': 'Curse' },
    'description' : 'Use this domain for words related to cursing someone.',
    'value' : '4.9.4.4 Curse'
  },
  {
    'guid' : '7c7f62d8-0293-45ba-8658-956c38bafc66',
    'id' : '4.9.4.5',
    'code' : '4.9.4.5',
    'abbr' : '4.9.4.5',
    'name' : { 'en': 'Destiny' },
    'description' : 'Use this domain for words related to destiny--decisions and actions by God, spirits, or by impersonal forces that determine or influence what happens to a person.',
    'value' : '4.9.4.5 Destiny'
  },
  {
    'guid' : '147c2e58-9ae8-460f-8cab-bf04a668945d',
    'id' : '4.9.4.6',
    'code' : '4.9.4.6',
    'abbr' : '4.9.4.6',
    'name' : { 'en': 'Prophecy' },
    'description' : 'Use this domain for words referring to speaking for God, including foretelling the future through divine knowledge.',
    'value' : '4.9.4.6 Prophecy'
  },
  {
    'guid' : 'b2830b72-c642-484f-9485-24682aa11ed8',
    'id' : '4.9.4.7',
    'code' : '4.9.4.7',
    'abbr' : '4.9.4.7',
    'name' : { 'en': 'Omen, divination' },
    'description' : 'Use this domain for words related to supernatural knowledge.',
    'value' : '4.9.4.7 Omen, divination'
  },
  {
    'guid' : 'b9c752a4-66be-493c-8955-cfa5324a54c1',
    'id' : '4.9.5',
    'code' : '4.9.5',
    'abbr' : '4.9.5',
    'name' : { 'en': 'Practice religion' },
    'description' : 'Use this domain for words related to practicing religion.',
    'value' : '4.9.5 Practice religion'
  },
  {
    'guid' : '4cb8b433-4efa-4698-8ebd-0f00f8fc3f66',
    'id' : '4.9.5.1',
    'code' : '4.9.5.1',
    'abbr' : '4.9.5.1',
    'name' : { 'en': 'Devout' },
    'description' : 'Use this domain for words related to being devout.',
    'value' : '4.9.5.1 Devout'
  },
  {
    'guid' : '00dde3be-e53d-42c3-b3ff-717e25cbffb6',
    'id' : '4.9.5.2',
    'code' : '4.9.5.2',
    'abbr' : '4.9.5.2',
    'name' : { 'en': 'Pray' },
    'description' : 'Use this domain for words related to praying--talking to God.',
    'value' : '4.9.5.2 Pray'
  },
  {
    'guid' : '2ccab97a-fb98-4054-a29b-e5ceac8ca1b4',
    'id' : '4.9.5.3',
    'code' : '4.9.5.3',
    'abbr' : '4.9.5.3',
    'name' : { 'en': 'Worship' },
    'description' : 'Use this domain for personal expressions of devotion to God, in whatever ways the religion defines and expresses it.',
    'value' : '4.9.5.3 Worship'
  },
  {
    'guid' : '5a4a8ae5-a209-4946-8fa1-3c3bd5083e0d',
    'id' : '4.9.5.4',
    'code' : '4.9.5.4',
    'abbr' : '4.9.5.4',
    'name' : { 'en': 'Religious ceremony' },
    'description' : 'Use this domain for words related to religious ceremonies.',
    'value' : '4.9.5.4 Religious ceremony'
  },
  {
    'guid' : '9f792202-8023-4ef3-b269-5ae4b6908a0b',
    'id' : '4.9.5.5',
    'code' : '4.9.5.5',
    'abbr' : '4.9.5.5',
    'name' : { 'en': 'Offering, sacrifice' },
    'description' : 'Use this domain for words related to offering a sacrifice.',
    'value' : '4.9.5.5 Offering, sacrifice'
  },
  {
    'guid' : 'b790470f-ed4e-42ac-932d-cd15ef701b03',
    'id' : '4.9.5.6',
    'code' : '4.9.5.6',
    'abbr' : '4.9.5.6',
    'name' : { 'en': 'Religious purification' },
    'description' : 'Use this domain for words related to religious purification.',
    'value' : '4.9.5.6 Religious purification'
  },
  {
    'guid' : '24361be2-49be-4860-bb56-4e46dd1e8b0c',
    'id' : '4.9.5.6.1',
    'code' : '4.9.5.6.1',
    'abbr' : '4.9.5.6.1',
    'name' : { 'en': 'Taboo' },
    'description' : 'Use this domain for words referring to things that are taboo--something to be avoided; a religious, social, or cultural restriction on behavior, as opposed to a government law.',
    'value' : '4.9.5.6.1 Taboo'
  },
  {
    'guid' : '6105a207-4311-4920-8c19-63259424bfaf',
    'id' : '4.9.5.7',
    'code' : '4.9.5.7',
    'abbr' : '4.9.5.7',
    'name' : { 'en': 'Salvation' },
    'description' : 'Use this domain for the primary goal or goals of a religion, for instance in Christianity salvation from sin, death and Hell. Each religion has different beliefs about salvation. Our purpose here is not to preach or argue, but to list those words that people use to talk about this topic.',
    'value' : '4.9.5.7 Salvation'
  },
  {
    'guid' : '62ed8254-e53a-4781-935e-79869619e40a',
    'id' : '4.9.5.8',
    'code' : '4.9.5.8',
    'abbr' : '4.9.5.8',
    'name' : { 'en': 'Dedicate to religious use' },
    'description' : 'Use this domain for words related to dedicating someone or something to religious use.',
    'value' : '4.9.5.8 Dedicate to religious use'
  },
  {
    'guid' : '79f3b53a-eb56-4188-87f8-48317f76e7ce',
    'id' : '4.9.5.9',
    'code' : '4.9.5.9',
    'abbr' : '4.9.5.9',
    'name' : { 'en': 'Fasting' },
    'description' : 'Use this domain for words related to fasting--to not eat for a period of time.',
    'value' : '4.9.5.9 Fasting'
  },
  {
    'guid' : '1c3f8996-362e-4ee0-af02-0dd02887f6aa',
    'id' : '4.9.6',
    'code' : '4.9.6',
    'abbr' : '4.9.6',
    'name' : { 'en': 'Heaven, hell' },
    'description' : 'Use this domain for words related to heaven and hell--the place where people go after they die.',
    'value' : '4.9.6 Heaven, hell'
  },
  {
    'guid' : 'ff505092-6d88-4b5e-8095-04e471d7ad4c',
    'id' : '4.9.6.1',
    'code' : '4.9.6.1',
    'abbr' : '4.9.6.1',
    'name' : { 'en': 'Resurrection' },
    'description' : 'Use this domain for words related to resurrection--life after death, or living again after dying.',
    'value' : '4.9.6.1 Resurrection'
  },
  {
    'guid' : '2470ad05-636e-4c85-96ab-cd880da58741',
    'id' : '4.9.7',
    'code' : '4.9.7',
    'abbr' : '4.9.7',
    'name' : { 'en': 'Religious organization' },
    'description' : 'Use this domain for words referring to official religions, groups within a religion, and religious meetings. Each religion will have different names for its groups. Answer each question for each religion.',
    'value' : '4.9.7 Religious organization'
  },
  {
    'guid' : 'ef860ee3-a4a5-4a42-b810-fdf41e35d151',
    'id' : '4.9.7.1',
    'code' : '4.9.7.1',
    'abbr' : '4.9.7.1',
    'name' : { 'en': 'Religious person' },
    'description' : 'Use this domain for words referring to religious practitioners--people who practice a religion, who are members of the religion, believe in the religion, leaders of the religion, and followers of the religion. Each religion has its own terms for religious practitioners. List these terms separately for each religion. The examples given below are for the Christian religion.',
    'value' : '4.9.7.1 Religious person'
  },
  {
    'guid' : '1229dd8f-5cfc-4644-93c3-d256fc34d054',
    'id' : '4.9.7.2',
    'code' : '4.9.7.2',
    'abbr' : '4.9.7.2',
    'name' : { 'en': 'Christianity' },
    'description' : 'Use this domain for words used in Christianity.',
    'value' : '4.9.7.2 Christianity'
  },
  {
    'guid' : 'ab8f5391-ad8b-42dc-a43c-22590a09ce77',
    'id' : '4.9.7.3',
    'code' : '4.9.7.3',
    'abbr' : '4.9.7.3',
    'name' : { 'en': 'Islam' },
    'description' : 'Use this domain for words used in Islam.',
    'value' : '4.9.7.3 Islam'
  },
  {
    'guid' : '4ce22ed0-6fe3-47ae-83e4-e7c7310cb1d4',
    'id' : '4.9.7.4',
    'code' : '4.9.7.4',
    'abbr' : '4.9.7.4',
    'name' : { 'en': 'Hinduism' },
    'description' : 'Use this domain for words used in Hinduism.',
    'value' : '4.9.7.4 Hinduism'
  },
  {
    'guid' : 'df647b1a-ed79-4a8e-b781-56ed25fe4405',
    'id' : '4.9.7.5',
    'code' : '4.9.7.5',
    'abbr' : '4.9.7.5',
    'name' : { 'en': 'Buddhism' },
    'description' : 'Use this domain for words used in Buddhism.',
    'value' : '4.9.7.5 Buddhism'
  },
  {
    'guid' : 'f211defe-d80f-4e40-9842-af19cb0719e7',
    'id' : '4.9.7.6',
    'code' : '4.9.7.6',
    'abbr' : '4.9.7.6',
    'name' : { 'en': 'Judaism' },
    'description' : 'Use this domain for words used in Judaism.',
    'value' : '4.9.7.6 Judaism'
  },
  {
    'guid' : 'f6134be5-3f96-4750-a03e-fca381a42db1',
    'id' : '4.9.7.7',
    'code' : '4.9.7.7',
    'abbr' : '4.9.7.7',
    'name' : { 'en': 'Animism ' },
    'description' : 'Use this domain for words used in Animism--the belief in spirits.',
    'value' : '4.9.7.7 Animism '
  },
  {
    'guid' : 'e311cc3a-a387-449e-a05a-07ed9678411d',
    'id' : '4.9.8',
    'code' : '4.9.8',
    'abbr' : '4.9.8',
    'name' : { 'en': 'Religious things' },
    'description' : 'Use this domain for words related to a religious object.',
    'value' : '4.9.8 Religious things'
  },
  {
    'guid' : 'b97531df-8256-4796-8335-f69753a8f2e3',
    'id' : '4.9.8.1',
    'code' : '4.9.8.1',
    'abbr' : '4.9.8.1',
    'name' : { 'en': 'Idol' },
    'description' : 'Use this domain for words related to idols and their use.',
    'value' : '4.9.8.1 Idol'
  },
  {
    'guid' : '9b476afd-58c2-46a2-8294-449ac4aad3a9',
    'id' : '4.9.8.2',
    'code' : '4.9.8.2',
    'abbr' : '4.9.8.2',
    'name' : { 'en': 'Place of worship' },
    'description' : 'Use this domain for words related to places of worship. Each religion has different types of places of worship. These questions must be answered according to the practices of each religion and for each separate type of place.',
    'value' : '4.9.8.2 Place of worship'
  },
  {
    'guid' : '725d78eb-8cac-4b2f-b5a4-f0a9adb80f5d',
    'id' : '4.9.9',
    'code' : '4.9.9',
    'abbr' : '4.9.9',
    'name' : { 'en': 'Irreligion' },
    'description' : 'Use this domain for words related to thinking and acting against God or religion.',
    'value' : '4.9.9 Irreligion'
  },
  {
    'guid' : '0f883eb0-00a1-44cc-b719-97fb6ec145d4',
    'id' : '5',
    'code' : '5',
    'abbr' : '5',
    'name' : { 'en': 'Daily life' },
    'description' : 'Use this domain for words related to daily life at home.',
    'value' : '5 Daily life'
  },
  {
    'guid' : 'bd4a2527-f66c-4f48-922e-8b180bba8ef6',
    'id' : '5.1',
    'code' : '5.1',
    'abbr' : '5.1',
    'name' : { 'en': 'Household equipment' },
    'description' : 'Use this domain for words related to household equipment and tools.',
    'value' : '5.1 Household equipment'
  },
  {
    'guid' : '44bf22fd-3725-4c49-bd3c-434402c33493',
    'id' : '5.1.1',
    'code' : '5.1.1',
    'abbr' : '5.1.1',
    'name' : { 'en': 'Furniture' },
    'description' : 'Use this domain for words related to furniture.',
    'value' : '5.1.1 Furniture'
  },
  {
    'guid' : 'f732bdb5-9a04-468a-b50b-510f94d20fb4',
    'id' : '5.1.1.1',
    'code' : '5.1.1.1',
    'abbr' : '5.1.1.1',
    'name' : { 'en': 'Table' },
    'description' : 'Use this domain for words related to a table.',
    'value' : '5.1.1.1 Table'
  },
  {
    'guid' : '4fc734f2-a91d-4693-8caf-e7fe51a2df8a',
    'id' : '5.1.1.2',
    'code' : '5.1.1.2',
    'abbr' : '5.1.1.2',
    'name' : { 'en': 'Chair' },
    'description' : 'Use this domain for words related to a chair.',
    'value' : '5.1.1.2 Chair'
  },
  {
    'guid' : '943eb131-2761-4c98-90a0-0bdfb0f8584d',
    'id' : '5.1.1.3',
    'code' : '5.1.1.3',
    'abbr' : '5.1.1.3',
    'name' : { 'en': 'Bed' },
    'description' : 'Use this domain for words related to a bed.',
    'value' : '5.1.1.3 Bed'
  },
  {
    'guid' : 'fe89a0f4-2155-424b-bf90-c1133dc41c8d',
    'id' : '5.1.1.4',
    'code' : '5.1.1.4',
    'abbr' : '5.1.1.4',
    'name' : { 'en': 'Cabinet' },
    'description' : 'Use this domain for words related to a cabinet.',
    'value' : '5.1.1.4 Cabinet'
  },
  {
    'guid' : 'a220a734-af03-4de3-8d05-369a3cad14cf',
    'id' : '5.1.2',
    'code' : '5.1.2',
    'abbr' : '5.1.2',
    'name' : { 'en': 'Household decoration' },
    'description' : 'Use this domain for words related to household decorations.',
    'value' : '5.1.2 Household decoration'
  },
  {
    'guid' : '4098899a-0ad0-4d71-9f9f-b99d5ba2e0d5',
    'id' : '5.2',
    'code' : '5.2',
    'abbr' : '5.2',
    'name' : { 'en': 'Food' },
    'description' : 'Use this domain for general words referring to food.',
    'value' : '5.2 Food'
  },
  {
    'guid' : '1fda68d4-5941-4695-b656-090d603a3344',
    'id' : '5.2.1',
    'code' : '5.2.1',
    'abbr' : '5.2.1',
    'name' : { 'en': 'Food preparation' },
    'description' : 'Use this domain for words related to food preparation.',
    'value' : '5.2.1 Food preparation'
  },
  {
    'guid' : 'fc0afb69-a4d4-439a-91cd-ed0ce67677b5',
    'id' : '5.2.1.1',
    'code' : '5.2.1.1',
    'abbr' : '5.2.1.1',
    'name' : { 'en': 'Cooking methods' },
    'description' : 'Use this domain for words referring to various ways of cooking food. It is necessary to think through different kinds of food and how they are cooked. An example is given below for cooking eggs.',
    'value' : '5.2.1.1 Cooking methods'
  },
  {
    'guid' : '726923d4-b25c-46eb-8ab0-427207177ae3',
    'id' : '5.2.1.2',
    'code' : '5.2.1.2',
    'abbr' : '5.2.1.2',
    'name' : { 'en': 'Steps in food preparation' },
    'description' : 'Use this domain for words related to the steps in food preparation. One way to find the words in this domain is to describe how each type of food is prepared.',
    'value' : '5.2.1.2 Steps in food preparation'
  },
  {
    'guid' : '6e83c471-0fe8-4641-8ecf-68b63df29ab7',
    'id' : '5.2.1.2.1',
    'code' : '5.2.1.2.1',
    'abbr' : '5.2.1.2.1',
    'name' : { 'en': 'Remove shell, skin' },
    'description' : 'Use this domain for words related to removing the shell or skin from food.',
    'value' : '5.2.1.2.1 Remove shell, skin'
  },
  {
    'guid' : '8a9b5484-eda8-4194-95ca-c2e76e83ae67',
    'id' : '5.2.1.2.2',
    'code' : '5.2.1.2.2',
    'abbr' : '5.2.1.2.2',
    'name' : { 'en': 'Pound in mortar and pestle' },
    'description' : 'Use this domain for words related to pounding food in a mortar.',
    'value' : '5.2.1.2.2 Pound in mortar and pestle'
  },
  {
    'guid' : 'ac7de73d-0059-4f35-9317-462a1813edba',
    'id' : '5.2.1.2.3',
    'code' : '5.2.1.2.3',
    'abbr' : '5.2.1.2.3',
    'name' : { 'en': 'Grind flour' },
    'description' : 'Use this domain for words related to grinding flour.',
    'value' : '5.2.1.2.3 Grind flour'
  },
  {
    'guid' : 'dbd3e164-3f70-4395-9728-1c24c8900da6',
    'id' : '5.2.1.3',
    'code' : '5.2.1.3',
    'abbr' : '5.2.1.3',
    'name' : { 'en': 'Cooking utensil' },
    'description' : 'Use this domain for words related to cooking utensils.',
    'value' : '5.2.1.3 Cooking utensil'
  },
  {
    'guid' : '1b399fa1-e4f7-4d7b-a33e-3972b8b556e2',
    'id' : '5.2.1.4',
    'code' : '5.2.1.4',
    'abbr' : '5.2.1.4',
    'name' : { 'en': 'Food storage' },
    'description' : 'Use this domain for words referring to food preservation and storage.',
    'value' : '5.2.1.4 Food storage'
  },
  {
    'guid' : 'cd01db6c-8aa6-42d1-93ac-05e81a8be523',
    'id' : '5.2.1.5',
    'code' : '5.2.1.5',
    'abbr' : '5.2.1.5',
    'name' : { 'en': 'Serve food' },
    'description' : 'Use this domain for words related to serving food.',
    'value' : '5.2.1.5 Serve food'
  },
  {
    'guid' : '0f568473-880d-43bd-b5ce-590100fdcaf6',
    'id' : '5.2.2',
    'code' : '5.2.2',
    'abbr' : '5.2.2',
    'name' : { 'en': 'Eat' },
    'description' : 'Use this domain for words related to eating.',
    'value' : '5.2.2 Eat'
  },
  {
    'guid' : '8242fc85-a703-4efa-a78a-0556a84e811e',
    'id' : '5.2.2.1',
    'code' : '5.2.2.1',
    'abbr' : '5.2.2.1',
    'name' : { 'en': 'Bite, chew' },
    'description' : 'Use this domain for words referring to biting and chewing with the teeth.',
    'value' : '5.2.2.1 Bite, chew'
  },
  {
    'guid' : '02d404d7-f3d7-492c-92a0-c6c9ff1a1908',
    'id' : '5.2.2.2',
    'code' : '5.2.2.2',
    'abbr' : '5.2.2.2',
    'name' : { 'en': 'Meal' },
    'description' : 'Use this domain for words related to a meal.',
    'value' : '5.2.2.2 Meal'
  },
  {
    'guid' : '08788e9a-93b8-4a2e-ab01-dea177f061e8',
    'id' : '5.2.2.3',
    'code' : '5.2.2.3',
    'abbr' : '5.2.2.3',
    'name' : { 'en': 'Feast' },
    'description' : 'Use this domain for words related to a feast.',
    'value' : '5.2.2.3 Feast'
  },
  {
    'guid' : '12b6934d-3a4a-4623-995f-865f401349ab',
    'id' : '5.2.2.4',
    'code' : '5.2.2.4',
    'abbr' : '5.2.2.4',
    'name' : { 'en': 'Manner of eating' },
    'description' : 'Use this domain for words describing the manner in which a person eats.',
    'value' : '5.2.2.4 Manner of eating'
  },
  {
    'guid' : '0a27d9d1-0f1f-475a-92a2-bbccf5b15f41',
    'id' : '5.2.2.5',
    'code' : '5.2.2.5',
    'abbr' : '5.2.2.5',
    'name' : { 'en': 'Hungry, thirsty' },
    'description' : 'Use this domain for words related to being hungry or thirsty.',
    'value' : '5.2.2.5 Hungry, thirsty'
  },
  {
    'guid' : '40ff5cee-31d8-4c89-a212-877347212a0e',
    'id' : '5.2.2.6',
    'code' : '5.2.2.6',
    'abbr' : '5.2.2.6',
    'name' : { 'en': 'Satiated, full' },
    'description' : 'Use this domain for words related to being full of food.',
    'value' : '5.2.2.6 Satiated, full'
  },
  {
    'guid' : '6de42a33-35b2-49c6-b2c4-fa9c0c5094f0',
    'id' : '5.2.2.7',
    'code' : '5.2.2.7',
    'abbr' : '5.2.2.7',
    'name' : { 'en': 'Drink' },
    'description' : 'Use this domain for words related to drinking.',
    'value' : '5.2.2.7 Drink'
  },
  {
    'guid' : 'b4e6c077-4f5e-44f3-8868-1f7ae3486585',
    'id' : '5.2.2.8',
    'code' : '5.2.2.8',
    'abbr' : '5.2.2.8',
    'name' : { 'en': 'Eating utensil' },
    'description' : 'Use this domain for words related to eating utensils.',
    'value' : '5.2.2.8 Eating utensil'
  },
  {
    'guid' : 'b3d3dd7d-0cb1-4c25-bcae-cc402fcfa3ea',
    'id' : '5.2.2.9',
    'code' : '5.2.2.9',
    'abbr' : '5.2.2.9',
    'name' : { 'en': 'Fast, not eat' },
    'description' : 'Use this domain for words related to fasting--to not eat for a period of time.',
    'value' : '5.2.2.9 Fast, not eat'
  },
  {
    'guid' : '5bcd08a6-cb46-41bb-a732-0d690b5ea596',
    'id' : '5.2.3',
    'code' : '5.2.3',
    'abbr' : '5.2.3',
    'name' : { 'en': 'Types of food' },
    'description' : 'Use this domain for words related to types of food.',
    'value' : '5.2.3 Types of food'
  },
  {
    'guid' : '32125c5f-d69a-442f-ba66-6277ec0a3b15',
    'id' : '5.2.3.1',
    'code' : '5.2.3.1',
    'abbr' : '5.2.3.1',
    'name' : { 'en': 'Food from plants' },
    'description' : 'Use this domain for words related to food from plants.',
    'value' : '5.2.3.1 Food from plants'
  },
  {
    'guid' : '1447278f-efff-4807-b9ea-c487dea1ba5e',
    'id' : '5.2.3.1.1',
    'code' : '5.2.3.1.1',
    'abbr' : '5.2.3.1.1',
    'name' : { 'en': 'Food from seeds' },
    'description' : 'Use this domain for words related to food from seeds.',
    'value' : '5.2.3.1.1 Food from seeds'
  },
  {
    'guid' : '9cae4ee3-03cf-46f7-9475-21d66c93ae04',
    'id' : '5.2.3.1.2',
    'code' : '5.2.3.1.2',
    'abbr' : '5.2.3.1.2',
    'name' : { 'en': 'Food from fruit' },
    'description' : 'Use this domain for words related to food from fruit.',
    'value' : '5.2.3.1.2 Food from fruit'
  },
  {
    'guid' : '929720f5-c264-49fd-b817-3e1ebff6e1de',
    'id' : '5.2.3.1.3',
    'code' : '5.2.3.1.3',
    'abbr' : '5.2.3.1.3',
    'name' : { 'en': 'Food from vegetables' },
    'description' : 'Use this domain for words related to food from vegetables.',
    'value' : '5.2.3.1.3 Food from vegetables'
  },
  {
    'guid' : 'e496a6d3-a00c-470e-81c3-314f3f97840e',
    'id' : '5.2.3.1.4',
    'code' : '5.2.3.1.4',
    'abbr' : '5.2.3.1.4',
    'name' : { 'en': 'Food from leaves' },
    'description' : 'Use this domain for words referring to food from leaves and stems.',
    'value' : '5.2.3.1.4 Food from leaves'
  },
  {
    'guid' : '462f5606-5bd8-4543-aa35-26b0cffd7163',
    'id' : '5.2.3.1.5',
    'code' : '5.2.3.1.5',
    'abbr' : '5.2.3.1.5',
    'name' : { 'en': 'Food from roots' },
    'description' : 'Use this domain for words referring to food from roots.',
    'value' : '5.2.3.1.5 Food from roots'
  },
  {
    'guid' : '1f608e18-958e-4bb3-a977-04879fb5acd5',
    'id' : '5.2.3.2',
    'code' : '5.2.3.2',
    'abbr' : '5.2.3.2',
    'name' : { 'en': 'Food from animals' },
    'description' : 'Use this domain for words referring to eating meat and to types of animals that are eaten. Only include those animals that are commonly eaten, especially those that are domesticated.',
    'value' : '5.2.3.2 Food from animals'
  },
  {
    'guid' : '5c348090-12f4-4c83-8331-e10971bbc8d3',
    'id' : '5.2.3.2.1',
    'code' : '5.2.3.2.1',
    'abbr' : '5.2.3.2.1',
    'name' : { 'en': 'Meat' },
    'description' : 'Use this domain for words referring to meat and types of animals that are eaten. Only include those animals that are commonly eaten.',
    'value' : '5.2.3.2.1 Meat'
  },
  {
    'guid' : 'd4769748-7c4e-4359-9da5-2ea64d5948d9',
    'id' : '5.2.3.2.2',
    'code' : '5.2.3.2.2',
    'abbr' : '5.2.3.2.2',
    'name' : { 'en': 'Milk products' },
    'description' : 'Use this domain for words related to milk products.',
    'value' : '5.2.3.2.2 Milk products'
  },
  {
    'guid' : 'aa5658da-8926-4519-83a5-d451dc5a6b49',
    'id' : '5.2.3.2.3',
    'code' : '5.2.3.2.3',
    'abbr' : '5.2.3.2.3',
    'name' : { 'en': 'Egg dishes' },
    'description' : 'Use this domain for words related to food made from eggs.',
    'value' : '5.2.3.2.3 Egg dishes'
  },
  {
    'guid' : '36ad58e3-ade7-49b9-9922-de0b5c3f13c3',
    'id' : '5.2.3.3',
    'code' : '5.2.3.3',
    'abbr' : '5.2.3.3',
    'name' : { 'en': 'Cooking ingredients' },
    'description' : 'Use this domain for general words referring to ingredients--the things that are added together when preparing food.',
    'value' : '5.2.3.3 Cooking ingredients'
  },
  {
    'guid' : '9bb6f1ed-7170-4caa-a14c-747fd95ca30e',
    'id' : '5.2.3.3.1',
    'code' : '5.2.3.3.1',
    'abbr' : '5.2.3.3.1',
    'name' : { 'en': 'Sugar' },
    'description' : 'Use this domain for words related to sugar.',
    'value' : '5.2.3.3.1 Sugar'
  },
  {
    'guid' : '7beca90c-3671-4b3c-bf9a-fb8f08ff914b',
    'id' : '5.2.3.3.2',
    'code' : '5.2.3.3.2',
    'abbr' : '5.2.3.3.2',
    'name' : { 'en': 'Salt' },
    'description' : 'Use this domain for words related to salt.',
    'value' : '5.2.3.3.2 Salt'
  },
  {
    'guid' : 'bf0b24d2-4bd6-4e9c-8775-a623ace8db56',
    'id' : '5.2.3.3.3',
    'code' : '5.2.3.3.3',
    'abbr' : '5.2.3.3.3',
    'name' : { 'en': 'Spice' },
    'description' : 'Use this domain for spices--things that are added to food to make them taste better.',
    'value' : '5.2.3.3.3 Spice'
  },
  {
    'guid' : 'e11d6360-6fa9-45a9-a23e-2252a301cf86',
    'id' : '5.2.3.3.4',
    'code' : '5.2.3.3.4',
    'abbr' : '5.2.3.3.4',
    'name' : { 'en': 'Leaven' },
    'description' : 'Use this domain for leaven--things that are added to food to make them ferment.',
    'value' : '5.2.3.3.4 Leaven'
  },
  {
    'guid' : 'c7c1c25a-d89d-4720-846c-d6e1dd723a17',
    'id' : '5.2.3.3.5',
    'code' : '5.2.3.3.5',
    'abbr' : '5.2.3.3.5',
    'name' : { 'en': 'Cooking oil' },
    'description' : 'Use this domain for words related to cooking oil.',
    'value' : '5.2.3.3.5 Cooking oil'
  },
  {
    'guid' : 'effc49dd-6322-4302-899c-4cf540f0e2e4',
    'id' : '5.2.3.4',
    'code' : '5.2.3.4',
    'abbr' : '5.2.3.4',
    'name' : { 'en': 'Prepared food' },
    'description' : 'Use this domain for words referring to prepared food. Cultures vary widely in the number and types of foods they prepare, and in how they classify them. For instance the English distinction between main dish and side dish is not found in the classification system of other languages. If your language has well-recognized subcategories, you can set up a separate subdomain for each. The questions below are based on the main ingredient in the dish.',
    'value' : '5.2.3.4 Prepared food'
  },
  {
    'guid' : '2eba12c6-7817-4dfd-9e7c-94c8b8b389ef',
    'id' : '5.2.3.5',
    'code' : '5.2.3.5',
    'abbr' : '5.2.3.5',
    'name' : { 'en': 'Prohibited food' },
    'description' : 'Use this domain for words that describe food that is prohibited by the culture or religion. Do not list the foods that are prohibited.',
    'value' : '5.2.3.5 Prohibited food'
  },
  {
    'guid' : '31dc3d15-c6f8-4405-a33b-8f3a52f8671a',
    'id' : '5.2.3.6',
    'code' : '5.2.3.6',
    'abbr' : '5.2.3.6',
    'name' : { 'en': 'Beverage' },
    'description' : 'Use this domain for words referring to things people drink.',
    'value' : '5.2.3.6 Beverage'
  },
  {
    'guid' : '2d894eca-8f6c-4b63-b265-0914a65d9be9',
    'id' : '5.2.3.7',
    'code' : '5.2.3.7',
    'abbr' : '5.2.3.7',
    'name' : { 'en': 'Alcoholic beverage' },
    'description' : 'Use this domain for types of beverages containing alcohol.',
    'value' : '5.2.3.7 Alcoholic beverage'
  },
  {
    'guid' : 'ee8a20b7-4202-489a-b8cd-bdebaf770313',
    'id' : '5.2.3.7.1',
    'code' : '5.2.3.7.1',
    'abbr' : '5.2.3.7.1',
    'name' : { 'en': 'Alcohol preparation' },
    'description' : 'Use this domain for words related to making alcoholic beverages.',
    'value' : '5.2.3.7.1 Alcohol preparation'
  },
  {
    'guid' : 'd067b555-e53c-4c16-bb09-5314862d8bae',
    'id' : '5.2.3.7.2',
    'code' : '5.2.3.7.2',
    'abbr' : '5.2.3.7.2',
    'name' : { 'en': 'Drunk' },
    'description' : 'Use this domain for words related to drinking alcohol and the effect it has on a person.',
    'value' : '5.2.3.7.2 Drunk'
  },
  {
    'guid' : 'c6688928-6694-4264-8048-a60b665b5793',
    'id' : '5.2.4',
    'code' : '5.2.4',
    'abbr' : '5.2.4',
    'name' : { 'en': 'Tobacco' },
    'description' : 'Use this domain for words related to using tobacco.',
    'value' : '5.2.4 Tobacco'
  },
  {
    'guid' : '3710e019-46c9-44db-a0aa-9054d3126161',
    'id' : '5.2.5',
    'code' : '5.2.5',
    'abbr' : '5.2.5',
    'name' : { 'en': 'Narcotic' },
    'description' : 'Use this domain for words related to narcotics and drugs that are not used as medicine but as stimulants. Narcotics are often addicting and harmful to a person"s health.',
    'value' : '5.2.5 Narcotic'
  },
  {
    'guid' : '029e0760-3306-41cc-b032-40befb22303e',
    'id' : '5.2.6',
    'code' : '5.2.6',
    'abbr' : '5.2.6',
    'name' : { 'en': 'Stimulant' },
    'description' : 'Use this domain for words related to stimulants--substances that are drunk, eaten, or chewed to make a person more alert or give him more energy.',
    'value' : '5.2.6 Stimulant'
  },
  {
    'guid' : '8a11e609-e88d-4247-8c5f-224ddb20de10',
    'id' : '5.3',
    'code' : '5.3',
    'abbr' : '5.3',
    'name' : { 'en': 'Clothing' },
    'description' : 'Use this domain for words related to clothing.',
    'value' : '5.3 Clothing'
  },
  {
    'guid' : 'ee6e993c-5551-42ae-b35e-26bc6aeeb3a4',
    'id' : '5.3.1',
    'code' : '5.3.1',
    'abbr' : '5.3.1',
    'name' : { 'en': 'Men"s clothing' },
    'description' : 'Use this domain for words related to men"s clothing.',
    'value' : '5.3.1 Men"s clothing'
  },
  {
    'guid' : '410a3d81-290f-416b-8012-3aa16eaa9e55',
    'id' : '5.3.2',
    'code' : '5.3.2',
    'abbr' : '5.3.2',
    'name' : { 'en': 'Women"s clothing' },
    'description' : 'Use this domain for words related to women"s clothing.',
    'value' : '5.3.2 Women"s clothing'
  },
  {
    'guid' : '5b41c1ed-95bb-4cca-8cff-87361acb5683',
    'id' : '5.3.3',
    'code' : '5.3.3',
    'abbr' : '5.3.3',
    'name' : { 'en': 'Traditional clothing' },
    'description' : 'Use this domain for words related to traditional clothing.',
    'value' : '5.3.3 Traditional clothing'
  },
  {
    'guid' : 'c3b808d4-d94e-4c8e-b7b2-87b4f4a83198',
    'id' : '5.3.4',
    'code' : '5.3.4',
    'abbr' : '5.3.4',
    'name' : { 'en': 'Clothes for special occasions' },
    'description' : 'Use this domain for words referring to special clothes worn on special occasions. It is necessary to think through all the various special occasions in the culture and think of any special clothes worn on those occasions. Two examples are given below for work and graduation, but there are many others.',
    'value' : '5.3.4 Clothes for special occasions'
  },
  {
    'guid' : 'c817af65-7cc8-4105-a8ed-47067d97b73b',
    'id' : '5.3.5',
    'code' : '5.3.5',
    'abbr' : '5.3.5',
    'name' : { 'en': 'Clothes for special people' },
    'description' : 'Use this domain for special clothes worn by special people. It is necessary to think through all the various special types of people in the culture and think of any special clothes worn by them. Several examples are given below, but there are many others.',
    'value' : '5.3.5 Clothes for special people'
  },
  {
    'guid' : '40516af2-d413-418e-8b68-8443847ee169',
    'id' : '5.3.6',
    'code' : '5.3.6',
    'abbr' : '5.3.6',
    'name' : { 'en': 'Parts of clothing' },
    'description' : 'Use this domain for words related to parts of clothing.',
    'value' : '5.3.6 Parts of clothing'
  },
  {
    'guid' : '4445cccd-e9b9-4f25-9e8c-2ef58408297d',
    'id' : '5.3.7',
    'code' : '5.3.7',
    'abbr' : '5.3.7',
    'name' : { 'en': 'Wear clothing' },
    'description' : 'Use this domain for words related to wearing clothing.',
    'value' : '5.3.7 Wear clothing'
  },
  {
    'guid' : '5450043d-907b-4884-a9e5-35cfd5935947',
    'id' : '5.3.8',
    'code' : '5.3.8',
    'abbr' : '5.3.8',
    'name' : { 'en': 'Naked' },
    'description' : 'Use this domain for words related to being naked--not wearing any clothes, and for words referring to how a person feels about being naked. It is a part of universal human experience that people do not want to be naked. So there are words that refer to feeling bad if one does not have enough clothes on (shame). Other words refer to wanting to have enough clothes (modest). Other words refer to how people feel about other people who do not wear enough clothes\n(indecent).',
    'value' : '5.3.8 Naked'
  },
  {
    'guid' : '72d9b7cd-aa06-4f7b-a66b-b992171d2cd4',
    'id' : '5.3.9',
    'code' : '5.3.9',
    'abbr' : '5.3.9',
    'name' : { 'en': 'Style of clothing' },
    'description' : 'Use this domain for words related to clothing styles.',
    'value' : '5.3.9 Style of clothing'
  },
  {
    'guid' : '15947464-997a-4f44-9a4b-ac4916e7e19b',
    'id' : '5.4',
    'code' : '5.4',
    'abbr' : '5.4',
    'name' : { 'en': 'Adornment' },
    'description' : 'Use this domain for words related to adornment.',
    'value' : '5.4 Adornment'
  },
  {
    'guid' : '844f922b-6fb6-49aa-864b-b1c49edaa1ae',
    'id' : '5.4.1',
    'code' : '5.4.1',
    'abbr' : '5.4.1',
    'name' : { 'en': 'Jewelry' },
    'description' : 'Use this domain for objects such as jewelry that are put on or attached to the body or to the clothes as decoration.',
    'value' : '5.4.1 Jewelry'
  },
  {
    'guid' : '5bdb3c06-fbee-4f5f-991a-e2128f65bb86',
    'id' : '5.4.2',
    'code' : '5.4.2',
    'abbr' : '5.4.2',
    'name' : { 'en': 'Cosmetics' },
    'description' : 'Use this domain for words related to cosmetics--things you put on your skin to make yourself beautiful in appearance.',
    'value' : '5.4.2 Cosmetics'
  },
  {
    'guid' : '6b8366b9-b5e8-42b8-991c-c568d4442a81',
    'id' : '5.4.3',
    'code' : '5.4.3',
    'abbr' : '5.4.3',
    'name' : { 'en': 'Care for hair' },
    'description' : 'Use this domain for words related to caring for your hair.',
    'value' : '5.4.3 Care for hair'
  },
  {
    'guid' : 'fc1e4ea7-15fa-4bbf-8697-f312762504ba',
    'id' : '5.4.3.1',
    'code' : '5.4.3.1',
    'abbr' : '5.4.3.1',
    'name' : { 'en': 'Comb hair' },
    'description' : 'Use this domain for words related to combing your hair.',
    'value' : '5.4.3.1 Comb hair'
  },
  {
    'guid' : 'b0e3486c-bd3d-4b5c-be9a-40cd2c0ce2a1',
    'id' : '5.4.3.2',
    'code' : '5.4.3.2',
    'abbr' : '5.4.3.2',
    'name' : { 'en': 'Plait hair' },
    'description' : 'Use this domain for words related to plaiting your hair.',
    'value' : '5.4.3.2 Plait hair'
  },
  {
    'guid' : '64fa0ba7-73cb-40e9-a8d2-3e61fff146c9',
    'id' : '5.4.3.3',
    'code' : '5.4.3.3',
    'abbr' : '5.4.3.3',
    'name' : { 'en': 'Dye hair' },
    'description' : 'Use this domain for words related to dying your hair.',
    'value' : '5.4.3.3 Dye hair'
  },
  {
    'guid' : '041b5ac9-99be-4281-a17e-654eff33d793',
    'id' : '5.4.3.4',
    'code' : '5.4.3.4',
    'abbr' : '5.4.3.4',
    'name' : { 'en': 'Hairstyle' },
    'description' : 'Use this domain for words related to hairstyles.',
    'value' : '5.4.3.4 Hairstyle'
  },
  {
    'guid' : '9c21f9bd-a7e0-4989-99f1-7fa2853ab73c',
    'id' : '5.4.3.5',
    'code' : '5.4.3.5',
    'abbr' : '5.4.3.5',
    'name' : { 'en': 'Cut hair' },
    'description' : 'Use this domain for words related to cutting hair.',
    'value' : '5.4.3.5 Cut hair'
  },
  {
    'guid' : '84d67c87-86ff-4e71-8a26-abe048132f8f',
    'id' : '5.4.3.6',
    'code' : '5.4.3.6',
    'abbr' : '5.4.3.6',
    'name' : { 'en': 'Shave' },
    'description' : 'Use this domain for words related to shaving.',
    'value' : '5.4.3.6 Shave'
  },
  {
    'guid' : 'c81004a7-499e-4e05-84c8-3d74a17e97fd',
    'id' : '5.4.4',
    'code' : '5.4.4',
    'abbr' : '5.4.4',
    'name' : { 'en': 'Care for the teeth' },
    'description' : 'Use this domain for words related to caring for your teeth.',
    'value' : '5.4.4 Care for the teeth'
  },
  {
    'guid' : '03e22b05-8505-442d-9c3b-7e691bd525e0',
    'id' : '5.4.5',
    'code' : '5.4.5',
    'abbr' : '5.4.5',
    'name' : { 'en': 'Anoint the body' },
    'description' : 'Use this domain for words related to anointing the body.',
    'value' : '5.4.5 Anoint the body'
  },
  {
    'guid' : '6ea9bfc6-723c-466f-9efc-0992879ae47d',
    'id' : '5.4.6',
    'code' : '5.4.6',
    'abbr' : '5.4.6',
    'name' : { 'en': 'Ritual scar' },
    'description' : 'Use this domain for words related to ritual scarring.',
    'value' : '5.4.6 Ritual scar'
  },
  {
    'guid' : '7f1dfb68-bf07-472d-a481-b802d2591ca6',
    'id' : '5.4.6.1',
    'code' : '5.4.6.1',
    'abbr' : '5.4.6.1',
    'name' : { 'en': 'Circumcision' },
    'description' : 'Use this domain for words related to circumcision.',
    'value' : '5.4.6.1 Circumcision'
  },
  {
    'guid' : '93de2257-8303-490d-b2fe-6d1d838b08c6',
    'id' : '5.4.7',
    'code' : '5.4.7',
    'abbr' : '5.4.7',
    'name' : { 'en': 'Care for the fingernails' },
    'description' : 'Use this domain for words related to caring for the fingernails.',
    'value' : '5.4.7 Care for the fingernails'
  },
  {
    'guid' : 'ff7e3abd-6810-4128-83c9-701b4925c2fe',
    'id' : '5.5',
    'code' : '5.5',
    'abbr' : '5.5',
    'name' : { 'en': 'Fire' },
    'description' : 'Use this domain for general words that refer to fire and for words referring to types of fire. These words may be specific for what is being burned (forest fire "a fire burning a forest"), the size of the fire (inferno "a very large hot fire"), or the place where the fire burns (hellfire "the fire in hell").',
    'value' : '5.5 Fire'
  },
  {
    'guid' : '1438c623-c4ce-4559-b71b-cfb86a71e6d7',
    'id' : '5.5.1',
    'code' : '5.5.1',
    'abbr' : '5.5.1',
    'name' : { 'en': 'Light a fire' },
    'description' : 'Use this domain for words related to lighting a fire.',
    'value' : '5.5.1 Light a fire'
  },
  {
    'guid' : '9a456463-3c71-4f9a-8117-dc2fcb087bd3',
    'id' : '5.5.2',
    'code' : '5.5.2',
    'abbr' : '5.5.2',
    'name' : { 'en': 'Tend a fire' },
    'description' : 'Use this domain for words that refer to tending a fire--to keep a fire burning so that it burns well and does not go out.',
    'value' : '5.5.2 Tend a fire'
  },
  {
    'guid' : '514974a2-c2fd-4b25-a24d-2ff52fa3d798',
    'id' : '5.5.3',
    'code' : '5.5.3',
    'abbr' : '5.5.3',
    'name' : { 'en': 'Extinguish a fire' },
    'description' : 'Use this domain for all the ways a person can stop a fire.',
    'value' : '5.5.3 Extinguish a fire'
  },
  {
    'guid' : '2933a9c1-aa62-46fb-a03c-68aed7fae9b7',
    'id' : '5.5.4',
    'code' : '5.5.4',
    'abbr' : '5.5.4',
    'name' : { 'en': 'Burn' },
    'description' : 'Use this domain for verbs that are used of fire: "The fire is ____." In some languages there are verbs for what is happening to the thing that is burning: "The house is ____."',
    'value' : '5.5.4 Burn'
  },
  {
    'guid' : 'b93bdfa4-486c-44a0-8266-557ccdc78b31',
    'id' : '5.5.5',
    'code' : '5.5.5',
    'abbr' : '5.5.5',
    'name' : { 'en': 'What fires produce' },
    'description' : 'Use this domain for words related to the things that fires produce.',
    'value' : '5.5.5 What fires produce'
  },
  {
    'guid' : '9fc25175-3b4a-4248-bc7f-b86012ec584f',
    'id' : '5.5.6',
    'code' : '5.5.6',
    'abbr' : '5.5.6',
    'name' : { 'en': 'Fuel' },
    'description' : 'Use this domain for types of fuel and for words used in making, collecting, storing, or using fuel. This domain includes the scenario of collecting firewood.',
    'value' : '5.5.6 Fuel'
  },
  {
    'guid' : '70164474-a65b-4506-9953-f26afb6b497a',
    'id' : '5.5.7',
    'code' : '5.5.7',
    'abbr' : '5.5.7',
    'name' : { 'en': 'Fireplace' },
    'description' : 'Use this domain for any place where fires are normally burned.',
    'value' : '5.5.7 Fireplace'
  },
  {
    'guid' : 'bbb897b5-f09b-4263-9f81-826ca61084f1',
    'id' : '5.6',
    'code' : '5.6',
    'abbr' : '5.6',
    'name' : { 'en': 'Cleaning' },
    'description' : 'Use this domain for general words related to cleaning things.',
    'value' : '5.6 Cleaning'
  },
  {
    'guid' : '46ad1505-9049-41d8-831b-768f46f12500',
    'id' : '5.6.1',
    'code' : '5.6.1',
    'abbr' : '5.6.1',
    'name' : { 'en': 'Clean, dirty' },
    'description' : 'Use this domain for words describing whether something is clean or dirty.',
    'value' : '5.6.1 Clean, dirty'
  },
  {
    'guid' : 'dd12ac0f-55cc-4c79-a50c-d23cc7ea60b3',
    'id' : '5.6.2',
    'code' : '5.6.2',
    'abbr' : '5.6.2',
    'name' : { 'en': 'Bathe' },
    'description' : 'Use this domain for words related to bathing.',
    'value' : '5.6.2 Bathe'
  },
  {
    'guid' : '9d79be9a-f21e-4189-be39-779db79da027',
    'id' : '5.6.3',
    'code' : '5.6.3',
    'abbr' : '5.6.3',
    'name' : { 'en': 'Wash dishes' },
    'description' : 'Use this domain for words related to washing dishes.',
    'value' : '5.6.3 Wash dishes'
  },
  {
    'guid' : 'dae6488c-7fea-4fa3-84c9-b611d017b6a5',
    'id' : '5.6.4',
    'code' : '5.6.4',
    'abbr' : '5.6.4',
    'name' : { 'en': 'Wash clothes' },
    'description' : 'Use this domain for words related to washing clothes.',
    'value' : '5.6.4 Wash clothes'
  },
  {
    'guid' : 'dc71598d-21a2-4598-9c12-13978796d2c9',
    'id' : '5.6.5',
    'code' : '5.6.5',
    'abbr' : '5.6.5',
    'name' : { 'en': 'Sweep, rake' },
    'description' : 'Use this domain for words related to cleaning the floor or ground.',
    'value' : '5.6.5 Sweep, rake'
  },
  {
    'guid' : 'c09eedd3-c4e1-4cf9-b6d1-a01624c6426a',
    'id' : '5.6.6',
    'code' : '5.6.6',
    'abbr' : '5.6.6',
    'name' : { 'en': 'Wipe, erase' },
    'description' : 'Use this domain for words related to wiping dirt off of things.',
    'value' : '5.6.6 Wipe, erase'
  },
  {
    'guid' : 'd502512c-966b-4752-8636-716fb29facfe',
    'id' : '5.7',
    'code' : '5.7',
    'abbr' : '5.7',
    'name' : { 'en': 'Sleep' },
    'description' : 'Use this domain for words related to sleeping.',
    'value' : '5.7 Sleep'
  },
  {
    'guid' : 'a3fe0ca2-64fe-44db-ac3f-14513385bc25',
    'id' : '5.7.1',
    'code' : '5.7.1',
    'abbr' : '5.7.1',
    'name' : { 'en': 'Go to sleep' },
    'description' : 'Use this domain for words related to going to bed and going to sleep.',
    'value' : '5.7.1 Go to sleep'
  },
  {
    'guid' : '6736dafe-2916-40f6-b6b7-b6300100933b',
    'id' : '5.7.2',
    'code' : '5.7.2',
    'abbr' : '5.7.2',
    'name' : { 'en': 'Dream' },
    'description' : 'Use this domain for words related to dreaming.',
    'value' : '5.7.2 Dream'
  },
  {
    'guid' : '2158eb7d-eb59-4740-9628-9080d7f51a97',
    'id' : '5.7.3',
    'code' : '5.7.3',
    'abbr' : '5.7.3',
    'name' : { 'en': 'Wake up' },
    'description' : 'Use this domain for words related to waking up from sleep.',
    'value' : '5.7.3 Wake up'
  },
  {
    'guid' : '716b3e9d-9bb9-42a6-ba56-829b1c018b28',
    'id' : '5.8',
    'code' : '5.8',
    'abbr' : '5.8',
    'name' : { 'en': 'Manage a house' },
    'description' : 'Use this domain for words related to managing a house.',
    'value' : '5.8 Manage a house'
  },
  {
    'guid' : 'f07f867d-808f-4750-92ca-859aea59e58c',
    'id' : '5.9',
    'code' : '5.9',
    'abbr' : '5.9',
    'name' : { 'en': 'Live, stay' },
    'description' : 'Use this domain for words related to living in a place.',
    'value' : '5.9 Live, stay'
  },
  {
    'guid' : 'c82fa28f-7e26-489e-a244-4d69cea87b94',
    'id' : '6',
    'code' : '6',
    'abbr' : '6',
    'name' : { 'en': 'Work and occupation' },
    'description' : 'Use this domain for general words related to working.',
    'value' : '6 Work and occupation'
  },
  {
    'guid' : 'd68911f6-6507-483a-b015-44726fdf868a',
    'id' : '6.1',
    'code' : '6.1',
    'abbr' : '6.1',
    'name' : { 'en': 'Work' },
    'description' : 'Use this domain for words related to working.',
    'value' : '6.1 Work'
  },
  {
    'guid' : '8be94377-a3cc-4187-a6e2-51523e953503',
    'id' : '6.1.1',
    'code' : '6.1.1',
    'abbr' : '6.1.1',
    'name' : { 'en': 'Worker' },
    'description' : 'Use this domain for words related to a worker.',
    'value' : '6.1.1 Worker'
  },
  {
    'guid' : 'b6ead5e6-dab5-4941-9017-d03452182709',
    'id' : '6.1.1.1',
    'code' : '6.1.1.1',
    'abbr' : '6.1.1.1',
    'name' : { 'en': 'Expert' },
    'description' : 'Use this domain for words related to being an expert--someone who can do something well.',
    'value' : '6.1.1.1 Expert'
  },
  {
    'guid' : 'd8366daf-ae1d-4b2c-a447-478c73580639',
    'id' : '6.1.2',
    'code' : '6.1.2',
    'abbr' : '6.1.2',
    'name' : { 'en': 'Method' },
    'description' : 'Use this domain for words related to the method of doing something.',
    'value' : '6.1.2 Method'
  },
  {
    'guid' : '3393b3b2-b324-408d-9c59-057a0de9c3bd',
    'id' : '6.1.2.1',
    'code' : '6.1.2.1',
    'abbr' : '6.1.2.1',
    'name' : { 'en': 'Try, attempt' },
    'description' : 'Use this domain for words indicating that someone is trying to do something.',
    'value' : '6.1.2.1 Try, attempt'
  },
  {
    'guid' : '64493789-1c2c-4b24-a7e6-f00ff9be923e',
    'id' : '6.1.2.2',
    'code' : '6.1.2.2',
    'abbr' : '6.1.2.2',
    'name' : { 'en': 'Use' },
    'description' : 'Use this domain for words related to using something to do something.',
    'value' : '6.1.2.2 Use'
  },
  {
    'guid' : '8411fa09-b1a5-4b62-aa47-f28bee9f6616',
    'id' : '6.1.2.2.1',
    'code' : '6.1.2.2.1',
    'abbr' : '6.1.2.2.1',
    'name' : { 'en': 'Useful' },
    'description' : 'Use this domain for words related to being useful--words describing something that can be used to do something.',
    'value' : '6.1.2.2.1 Useful'
  },
  {
    'guid' : '29d131d2-5e52-49e3-83b1-c872d331cf03',
    'id' : '6.1.2.2.2',
    'code' : '6.1.2.2.2',
    'abbr' : '6.1.2.2.2',
    'name' : { 'en': 'Useless' },
    'description' : 'Use this domain for words related to being useless--words describing something that cannot be used to do anything.',
    'value' : '6.1.2.2.2 Useless'
  },
  {
    'guid' : '34dd26b6-d081-42f5-8be9-c5fe7a7253b2',
    'id' : '6.1.2.2.3',
    'code' : '6.1.2.2.3',
    'abbr' : '6.1.2.2.3',
    'name' : { 'en': 'Available' },
    'description' : 'Use this domain for words related to something being available to use.',
    'value' : '6.1.2.2.3 Available'
  },
  {
    'guid' : 'b4dc89dc-3811-4d45-b0ee-0b71e28305cc',
    'id' : '6.1.2.2.4',
    'code' : '6.1.2.2.4',
    'abbr' : '6.1.2.2.4',
    'name' : { 'en': 'Use up' },
    'description' : 'Use this domain for words related to using something up.',
    'value' : '6.1.2.2.4 Use up'
  },
  {
    'guid' : '424ced39-d801-419a-86fe-265942a9b74b',
    'id' : '6.1.2.2.5',
    'code' : '6.1.2.2.5',
    'abbr' : '6.1.2.2.5',
    'name' : { 'en': 'Take care of something' },
    'description' : 'Use this domain for words related to taking care of something.',
    'value' : '6.1.2.2.5 Take care of something'
  },
  {
    'guid' : 'a72515ec-998d-4b48-bd69-c67cf9245abc',
    'id' : '6.1.2.2.6',
    'code' : '6.1.2.2.6',
    'abbr' : '6.1.2.2.6',
    'name' : { 'en': 'Waste' },
    'description' : 'Use this domain for words related to wasting something.',
    'value' : '6.1.2.2.6 Waste'
  },
  {
    'guid' : 'd6c29733-beeb-4fc9-975f-5e78a8acc273',
    'id' : '6.1.2.3',
    'code' : '6.1.2.3',
    'abbr' : '6.1.2.3',
    'name' : { 'en': 'Work well' },
    'description' : 'Use this domain for words related to working well.',
    'value' : '6.1.2.3 Work well'
  },
  {
    'guid' : '5b3c7b4d-d5bb-488e-8f3f-fe8206fb0e55',
    'id' : '6.1.2.3.1',
    'code' : '6.1.2.3.1',
    'abbr' : '6.1.2.3.1',
    'name' : { 'en': 'Careful' },
    'description' : 'Use this domain for words related to being careful.',
    'value' : '6.1.2.3.1 Careful'
  },
  {
    'guid' : '398ffed0-bfa7-452c-8521-7d37b3082dcf',
    'id' : '6.1.2.3.2',
    'code' : '6.1.2.3.2',
    'abbr' : '6.1.2.3.2',
    'name' : { 'en': 'Work hard' },
    'description' : 'Use this domain for words related to working hard.',
    'value' : '6.1.2.3.2 Work hard'
  },
  {
    'guid' : '0d935e77-e437-426f-acff-dccfb516ec8c',
    'id' : '6.1.2.3.3',
    'code' : '6.1.2.3.3',
    'abbr' : '6.1.2.3.3',
    'name' : { 'en': 'Busy' },
    'description' : 'Use this domain for words related to being busy.',
    'value' : '6.1.2.3.3 Busy'
  },
  {
    'guid' : '303539ba-7253-4590-b8c4-7751caa52c65',
    'id' : '6.1.2.3.4',
    'code' : '6.1.2.3.4',
    'abbr' : '6.1.2.3.4',
    'name' : { 'en': 'Power, force' },
    'description' : 'Use this domain for words related to the power used to do something.',
    'value' : '6.1.2.3.4 Power, force'
  },
  {
    'guid' : '80f26fbc-ca89-4789-996d-4c09547f2504',
    'id' : '6.1.2.3.5',
    'code' : '6.1.2.3.5',
    'abbr' : '6.1.2.3.5',
    'name' : { 'en': 'Complete, finish' },
    'description' : 'Use this domain for words related to completing a task.',
    'value' : '6.1.2.3.5 Complete, finish'
  },
  {
    'guid' : '5f63805f-1c8e-440a-a13c-222c5d81eb9c',
    'id' : '6.1.2.3.6',
    'code' : '6.1.2.3.6',
    'abbr' : '6.1.2.3.6',
    'name' : { 'en': 'Ambitious' },
    'description' : 'Use this domain for words related to being ambitious.',
    'value' : '6.1.2.3.6 Ambitious'
  },
  {
    'guid' : 'b760a3a7-ea7f-4a4b-a4b5-81752f2ca158',
    'id' : '6.1.2.4',
    'code' : '6.1.2.4',
    'abbr' : '6.1.2.4',
    'name' : { 'en': 'Work poorly' },
    'description' : 'Use this domain for words related to working poorly.',
    'value' : '6.1.2.4 Work poorly'
  },
  {
    'guid' : '6e04a3e2-3b3a-4d0b-bf71-1596ca0aa211',
    'id' : '6.1.2.4.1',
    'code' : '6.1.2.4.1',
    'abbr' : '6.1.2.4.1',
    'name' : { 'en': 'Careless, irresponsible' },
    'description' : 'Use this domain for words related to being careless.',
    'value' : '6.1.2.4.1 Careless, irresponsible'
  },
  {
    'guid' : '0c21ae3d-10b1-481f-8d8f-66e2590c4578',
    'id' : '6.1.2.4.2',
    'code' : '6.1.2.4.2',
    'abbr' : '6.1.2.4.2',
    'name' : { 'en': 'Lazy' },
    'description' : 'Use this domain for words related to being lazy.',
    'value' : '6.1.2.4.2 Lazy'
  },
  {
    'guid' : 'a2ae0c29-df1c-486d-a44b-96fcc4e0aa8c',
    'id' : '6.1.2.4.3',
    'code' : '6.1.2.4.3',
    'abbr' : '6.1.2.4.3',
    'name' : { 'en': 'Give up' },
    'description' : 'Use this domain for words related to giving up.',
    'value' : '6.1.2.4.3 Give up'
  },
  {
    'guid' : '53ba3b61-4f4e-4749-8a7f-0d2b327a113d',
    'id' : '6.1.2.5',
    'code' : '6.1.2.5',
    'abbr' : '6.1.2.5',
    'name' : { 'en': 'Plan' },
    'description' : 'Use this domain for words related to planning.',
    'value' : '6.1.2.5 Plan'
  },
  {
    'guid' : 'da203891-90a2-48f0-955a-8a80b6c62af9',
    'id' : '6.1.2.5.1',
    'code' : '6.1.2.5.1',
    'abbr' : '6.1.2.5.1',
    'name' : { 'en': 'Arrange an event' },
    'description' : 'Use this domain for words related to arranging an event, such as a meeting.',
    'value' : '6.1.2.5.1 Arrange an event'
  },
  {
    'guid' : '6fceec81-1967-4fe5-81f3-86bcaf3f1c2f',
    'id' : '6.1.2.5.2',
    'code' : '6.1.2.5.2',
    'abbr' : '6.1.2.5.2',
    'name' : { 'en': 'Cancel an event' },
    'description' : 'Use this domain for words related to canceling a plan, decision, or event.',
    'value' : '6.1.2.5.2 Cancel an event'
  },
  {
    'guid' : '889de305-82ad-4d1a-9a97-63733ed27bfc',
    'id' : '6.1.2.6',
    'code' : '6.1.2.6',
    'abbr' : '6.1.2.6',
    'name' : { 'en': 'Prepare' },
    'description' : 'Use this domain for words related to preparing to do something.',
    'value' : '6.1.2.6 Prepare'
  },
  {
    'guid' : 'f77053f4-ed5a-4376-bcba-17552ea447ba',
    'id' : '6.1.2.6.1',
    'code' : '6.1.2.6.1',
    'abbr' : '6.1.2.6.1',
    'name' : { 'en': 'Prepare something for use' },
    'description' : 'Use this domain for words related to preparing something so that it can be used for some purpose.',
    'value' : '6.1.2.6.1 Prepare something for use'
  },
  {
    'guid' : '6eb2ef50-7e6b-41b8-a82e-6eb307c908ca',
    'id' : '6.1.2.7',
    'code' : '6.1.2.7',
    'abbr' : '6.1.2.7',
    'name' : { 'en': 'Effective' },
    'description' : 'Use this domain for words related to being effective.',
    'value' : '6.1.2.7 Effective'
  },
  {
    'guid' : '6dd16e6e-fbdb-4df1-9730-4877651e68f3',
    'id' : '6.1.2.8',
    'code' : '6.1.2.8',
    'abbr' : '6.1.2.8',
    'name' : { 'en': 'Efficient' },
    'description' : 'Use this domain for words related to being efficient.',
    'value' : '6.1.2.8 Efficient'
  },
  {
    'guid' : 'a7568031-43e3-4cf9-b162-ac2fe74125f1',
    'id' : '6.1.2.9',
    'code' : '6.1.2.9',
    'abbr' : '6.1.2.9',
    'name' : { 'en': 'Opportunity' },
    'description' : 'Use this domain for words related to an opportunity to do something.',
    'value' : '6.1.2.9 Opportunity'
  },
  {
    'guid' : '31ccb9e3-d434-4430-ac84-486cc5a1c53d',
    'id' : '6.1.3',
    'code' : '6.1.3',
    'abbr' : '6.1.3',
    'name' : { 'en': 'Difficult, impossible' },
    'description' : 'Use this domain for words describing something that is difficult or impossible to do.',
    'value' : '6.1.3 Difficult, impossible'
  },
  {
    'guid' : '77bcdcab-e9fd-48ba-8dcd-63f425367735',
    'id' : '6.1.3.1',
    'code' : '6.1.3.1',
    'abbr' : '6.1.3.1',
    'name' : { 'en': 'Easy, possible' },
    'description' : 'Use this domain for words describing something that is easy or possible to do.',
    'value' : '6.1.3.1 Easy, possible'
  },
  {
    'guid' : '58eeb55b-c57d-4f59-a7d6-9bf663fbf831',
    'id' : '6.1.3.2',
    'code' : '6.1.3.2',
    'abbr' : '6.1.3.2',
    'name' : { 'en': 'Succeed' },
    'description' : 'Use this domain for words related to succeeding in doing something.',
    'value' : '6.1.3.2 Succeed'
  },
  {
    'guid' : 'a764174f-ccb7-48b1-add7-158bc89a5e81',
    'id' : '6.1.3.3',
    'code' : '6.1.3.3',
    'abbr' : '6.1.3.3',
    'name' : { 'en': 'Fail' },
    'description' : 'Use this domain for words related to failing to do something.',
    'value' : '6.1.3.3 Fail'
  },
  {
    'guid' : '7c6ba6e5-d81e-4a50-a111-c946a0793378',
    'id' : '6.1.3.4',
    'code' : '6.1.3.4',
    'abbr' : '6.1.3.4',
    'name' : { 'en': 'Advantage' },
    'description' : 'Use this domain for words related to having an advantage--something that helps you succeed that other people don"t have.',
    'value' : '6.1.3.4 Advantage'
  },
  {
    'guid' : '2e9f06f3-c986-43da-a035-e3cc9aef13d4',
    'id' : '6.1.4',
    'code' : '6.1.4',
    'abbr' : '6.1.4',
    'name' : { 'en': 'Job satisfaction' },
    'description' : 'Use this domain for words related to being satisfied with your job.',
    'value' : '6.1.4 Job satisfaction'
  },
  {
    'guid' : 'b285bc3b-ba9f-4160-8e79-81dd43dfdbaa',
    'id' : '6.1.5',
    'code' : '6.1.5',
    'abbr' : '6.1.5',
    'name' : { 'en': 'Unemployed, not working' },
    'description' : 'Use this domain for words related to being unemployed.',
    'value' : '6.1.5 Unemployed, not working'
  },
  {
    'guid' : '67247ab3-7b3a-4035-a0d0-a05c8e615c71',
    'id' : '6.1.6',
    'code' : '6.1.6',
    'abbr' : '6.1.6',
    'name' : { 'en': 'Made by hand' },
    'description' : 'Use this domain for words describing something made by hand instead of by a machine.',
    'value' : '6.1.6 Made by hand'
  },
  {
    'guid' : '97f40359-f4e8-4545-9ba5-980b47487540',
    'id' : '6.1.7',
    'code' : '6.1.7',
    'abbr' : '6.1.7',
    'name' : { 'en': 'Artificial' },
    'description' : 'Use this domain for words describing something that is artificial--something that is made by people.',
    'value' : '6.1.7 Artificial'
  },
  {
    'guid' : '5d92b4a7-baf7-495e-bb43-4f733cc55935',
    'id' : '6.1.8',
    'code' : '6.1.8',
    'abbr' : '6.1.8',
    'name' : { 'en': 'Experienced' },
    'description' : 'Use this domain for words related to being experienced at doing something.',
    'value' : '6.1.8 Experienced'
  },
  {
    'guid' : 'ea839451-f89e-4432-b361-3086ca4f13fd',
    'id' : '6.1.8.1',
    'code' : '6.1.8.1',
    'abbr' : '6.1.8.1',
    'name' : { 'en': 'Accustomed to' },
    'description' : 'Use this domain for words related to being accustomed to something.',
    'value' : '6.1.8.1 Accustomed to'
  },
  {
    'guid' : '5bb29704-fe0f-4594-9622-ce0aa42b93c8',
    'id' : '6.2',
    'code' : '6.2',
    'abbr' : '6.2',
    'name' : { 'en': 'Agriculture' },
    'description' : 'Use this domain for words related to agriculture--working with plants.',
    'value' : '6.2 Agriculture'
  },
  {
    'guid' : 'cb362fc7-b3aa-46f4-b9a8-0f8c97fb16fe',
    'id' : '6.2.1',
    'code' : '6.2.1',
    'abbr' : '6.2.1',
    'name' : { 'en': 'Growing crops' },
    'description' : 'Use this domain for general words related to growing crops. If one crop is cultivated extensively and there are many words related to it, set up a separate domain for it.',
    'value' : '6.2.1 Growing crops'
  },
  {
    'guid' : '36a2c83f-f7aa-41b0-9b17-f801f3720e4f',
    'id' : '6.2.1.1',
    'code' : '6.2.1.1',
    'abbr' : '6.2.1.1',
    'name' : { 'en': 'Growing grain' },
    'description' : 'Use this domain for general words related to growing grain crops such as barley, maize (corn), millet, oats, rice, rye, sesame, sorghum, and wheat. If one crop is cultivated extensively and there are many words related to it, set up a separate domain for it. This has already been done for rice, wheat, and maize, since they are so common around the world.',
    'value' : '6.2.1.1 Growing grain'
  },
  {
    'guid' : '7d7bc686-faf5-484b-8519-b2529ac581bf',
    'id' : '6.2.1.1.1',
    'code' : '6.2.1.1.1',
    'abbr' : '6.2.1.1.1',
    'name' : { 'en': 'Growing rice' },
    'description' : 'Use this domain for words related to growing rice.',
    'value' : '6.2.1.1.1 Growing rice'
  },
  {
    'guid' : 'd7e4e538-039f-47bb-aa42-a2cf455668cc',
    'id' : '6.2.1.1.2',
    'code' : '6.2.1.1.2',
    'abbr' : '6.2.1.1.2',
    'name' : { 'en': 'Growing wheat' },
    'description' : 'Use this domain for words related to growing wheat.',
    'value' : '6.2.1.1.2 Growing wheat'
  },
  {
    'guid' : '37f6a1d9-985d-465e-b62d-37c1f9bf855b',
    'id' : '6.2.1.1.3',
    'code' : '6.2.1.1.3',
    'abbr' : '6.2.1.1.3',
    'name' : { 'en': 'Growing maize' },
    'description' : 'Use this domain for words related to growing maize.',
    'value' : '6.2.1.1.3 Growing maize'
  },
  {
    'guid' : '7524887a-5cf0-4459-96d1-fd8262bef7d4',
    'id' : '6.2.1.2',
    'code' : '6.2.1.2',
    'abbr' : '6.2.1.2',
    'name' : { 'en': 'Growing roots' },
    'description' : 'Use this domain for words related to growing root crops such as beets, carrots, cassava, garlic, ginger, leeks, manioc, onions, peanuts, potatoes, radishes, rutabagas, taro, turnips, and yams. If one crop is cultivated extensively and there are many words related to it, set up a separate domain for it. This has already been done for potatoes and cassava, since they are so common around the world.',
    'value' : '6.2.1.2 Growing roots'
  },
  {
    'guid' : 'c982941c-0cff-47aa-9e08-5234a8e0d6e8',
    'id' : '6.2.1.2.1',
    'code' : '6.2.1.2.1',
    'abbr' : '6.2.1.2.1',
    'name' : { 'en': 'Growing potatoes' },
    'description' : 'Use this domain for words related to growing potatoes.',
    'value' : '6.2.1.2.1 Growing potatoes'
  },
  {
    'guid' : '139cd00c-429c-465a-a227-512af0c48039',
    'id' : '6.2.1.2.2',
    'code' : '6.2.1.2.2',
    'abbr' : '6.2.1.2.2',
    'name' : { 'en': 'Growing cassava' },
    'description' : 'Use this domain for words related to growing cassava.',
    'value' : '6.2.1.2.2 Growing cassava'
  },
  {
    'guid' : '2854734e-834a-42cb-8812-d9e7028916dc',
    'id' : '6.2.1.3',
    'code' : '6.2.1.3',
    'abbr' : '6.2.1.3',
    'name' : { 'en': 'Growing vegetables' },
    'description' : 'Use this domain for words related to growing vegetables, such as asparagus, beans, broccoli, cabbage, celery, chard, cucumbers, eggplant, melons, peas, peppers, pumpkins, spinach, squash, tomatoes, and watermelons. If one type of vegetable is cultivated extensively and there are many words related to it, set up a separate domain for it.',
    'value' : '6.2.1.3 Growing vegetables'
  },
  {
    'guid' : 'af5bcf27-56e2-4072-b321-30a31b58af78',
    'id' : '6.2.1.4',
    'code' : '6.2.1.4',
    'abbr' : '6.2.1.4',
    'name' : { 'en': 'Growing fruit' },
    'description' : 'Use this domain for words related to growing fruit, such as berries, cranberries, grapes, raspberries, and strawberries. If one type of fruit is cultivated extensively and there are many words related to it, set up a separate domain for it. This has already been done for grapes and bananas, since they are so common around the world.',
    'value' : '6.2.1.4 Growing fruit'
  },
  {
    'guid' : '7d9f48f5-aba3-486f-b49d-cd2cb0ac03f8',
    'id' : '6.2.1.4.1',
    'code' : '6.2.1.4.1',
    'abbr' : '6.2.1.4.1',
    'name' : { 'en': 'Growing grapes' },
    'description' : 'Use this domain for words related to growing grapes.',
    'value' : '6.2.1.4.1 Growing grapes'
  },
  {
    'guid' : 'b7662b2b-e57c-400b-8ef6-6fb612f5ee9f',
    'id' : '6.2.1.4.2',
    'code' : '6.2.1.4.2',
    'abbr' : '6.2.1.4.2',
    'name' : { 'en': 'Growing bananas' },
    'description' : 'Use this domain for words related to growing bananas.',
    'value' : '6.2.1.4.2 Growing bananas'
  },
  {
    'guid' : '198436ae-c3c6-4f3c-8fe0-ea10c867f1c6',
    'id' : '6.2.1.5',
    'code' : '6.2.1.5',
    'abbr' : '6.2.1.5',
    'name' : { 'en': 'Growing grass' },
    'description' : 'Use this domain for words related to growing grass, such as sod, hay, alfalfa, bamboo, papyrus, sugarcane, and tobacco. If one type of grass is cultivated extensively and there are many words related to it, set up a separate domain for it. This has already been done for sugarcane and tobacco, since they are so common around the world.',
    'value' : '6.2.1.5 Growing grass'
  },
  {
    'guid' : '31426c31-9439-406c-9867-bc98c6ca0565',
    'id' : '6.2.1.5.1',
    'code' : '6.2.1.5.1',
    'abbr' : '6.2.1.5.1',
    'name' : { 'en': 'Growing sugarcane' },
    'description' : 'Use this domain for words related to growing sugarcane.',
    'value' : '6.2.1.5.1 Growing sugarcane'
  },
  {
    'guid' : '99d0d129-b427-4468-b4c6-91005be63e18',
    'id' : '6.2.1.5.2',
    'code' : '6.2.1.5.2',
    'abbr' : '6.2.1.5.2',
    'name' : { 'en': 'Growing tobacco' },
    'description' : 'Use this domain for words related to growing tobacco.',
    'value' : '6.2.1.5.2 Growing tobacco'
  },
  {
    'guid' : 'bf1f9360-6dd4-4ee3-b9f8-a5539baeb53b',
    'id' : '6.2.1.6',
    'code' : '6.2.1.6',
    'abbr' : '6.2.1.6',
    'name' : { 'en': 'Growing flowers' },
    'description' : 'Use this domain for words related to growing flowers. If one type of flower is cultivated extensively and there are many words related to it, set up a separate domain for it.',
    'value' : '6.2.1.6 Growing flowers'
  },
  {
    'guid' : 'ef5ee2be-8a32-452a-818b-80191edb8e41',
    'id' : '6.2.1.7',
    'code' : '6.2.1.7',
    'abbr' : '6.2.1.7',
    'name' : { 'en': 'Growing trees' },
    'description' : 'Use this domain for words related to growing trees. If one crop is cultivated extensively and there are many words related to it, set up a separate domain for it. This has already been done for coconuts and coffee, since they are so common around the world.',
    'value' : '6.2.1.7 Growing trees'
  },
  {
    'guid' : '18043b8c-3ff0-46a5-87cc-626f62f967cc',
    'id' : '6.2.1.7.1',
    'code' : '6.2.1.7.1',
    'abbr' : '6.2.1.7.1',
    'name' : { 'en': 'Growing coconuts' },
    'description' : 'Use this domain for words related to growing coconuts.',
    'value' : '6.2.1.7.1 Growing coconuts'
  },
  {
    'guid' : '22300e2c-3d7d-4c36-a2b7-e2bbb247f793',
    'id' : '6.2.1.7.2',
    'code' : '6.2.1.7.2',
    'abbr' : '6.2.1.7.2',
    'name' : { 'en': 'Growing coffee' },
    'description' : 'Use this domain for words related to growing coffee.',
    'value' : '6.2.1.7.2 Growing coffee'
  },
  {
    'guid' : 'd345142f-d51e-4023-a25a-2a4c0f1fbcbf',
    'id' : '6.2.2',
    'code' : '6.2.2',
    'abbr' : '6.2.2',
    'name' : { 'en': 'Land preparation' },
    'description' : 'Use this domain for words related to preparing land for planting crops.',
    'value' : '6.2.2 Land preparation'
  },
  {
    'guid' : '685d474d-3b84-4339-98c6-2aac28b9870c',
    'id' : '6.2.2.1',
    'code' : '6.2.2.1',
    'abbr' : '6.2.2.1',
    'name' : { 'en': 'Clear a field' },
    'description' : 'Use this domain for words related to clearing a field.',
    'value' : '6.2.2.1 Clear a field'
  },
  {
    'guid' : '62030451-c0f2-4e80-8085-05c6400fc914',
    'id' : '6.2.2.2',
    'code' : '6.2.2.2',
    'abbr' : '6.2.2.2',
    'name' : { 'en': 'Plow a field' },
    'description' : 'Use this domain for words related to plowing a field.',
    'value' : '6.2.2.2 Plow a field'
  },
  {
    'guid' : '58de3766-8729-48e2-97fe-937a441eb722',
    'id' : '6.2.2.3',
    'code' : '6.2.2.3',
    'abbr' : '6.2.2.3',
    'name' : { 'en': 'Fertilize a field' },
    'description' : 'Use this domain for words related to fertilizing a field.',
    'value' : '6.2.2.3 Fertilize a field'
  },
  {
    'guid' : 'be280123-dda6-49a0-bd8c-5e2855b56159',
    'id' : '6.2.3',
    'code' : '6.2.3',
    'abbr' : '6.2.3',
    'name' : { 'en': 'Plant a field' },
    'description' : 'Use this domain for words related to planting a field.',
    'value' : '6.2.3 Plant a field'
  },
  {
    'guid' : '811e8c93-d97a-4aab-bd67-268b7783ff11',
    'id' : '6.2.4',
    'code' : '6.2.4',
    'abbr' : '6.2.4',
    'name' : { 'en': 'Tend a field' },
    'description' : 'Use this domain for words related to tending a field.',
    'value' : '6.2.4 Tend a field'
  },
  {
    'guid' : '32f868e0-54a7-4d04-8689-ac10e13396e5',
    'id' : '6.2.4.1',
    'code' : '6.2.4.1',
    'abbr' : '6.2.4.1',
    'name' : { 'en': 'Cut grass' },
    'description' : 'Use this domain for words related to cutting grass.',
    'value' : '6.2.4.1 Cut grass'
  },
  {
    'guid' : '9e98c76f-6c1c-4b7d-8f71-38f9f0e8750e',
    'id' : '6.2.4.2',
    'code' : '6.2.4.2',
    'abbr' : '6.2.4.2',
    'name' : { 'en': 'Uproot plants' },
    'description' : 'Use this domain for words related to uprooting plants.',
    'value' : '6.2.4.2 Uproot plants'
  },
  {
    'guid' : 'a2ce1453-9832-447c-9481-70c9e2da4227',
    'id' : '6.2.4.3',
    'code' : '6.2.4.3',
    'abbr' : '6.2.4.3',
    'name' : { 'en': 'Irrigate' },
    'description' : 'Use this domain for words related to irrigating a field.',
    'value' : '6.2.4.3 Irrigate'
  },
  {
    'guid' : 'a9fe3347-12ae-4624-b55e-45ea42cdbf9b',
    'id' : '6.2.4.4',
    'code' : '6.2.4.4',
    'abbr' : '6.2.4.4',
    'name' : { 'en': 'Trim plants' },
    'description' : 'Use this domain for words related to trimming plants.',
    'value' : '6.2.4.4 Trim plants'
  },
  {
    'guid' : '0698b0f4-0a31-4a70-9262-8d36677d8faa',
    'id' : '6.2.4.5',
    'code' : '6.2.4.5',
    'abbr' : '6.2.4.5',
    'name' : { 'en': 'Neglect plants' },
    'description' : 'Use this domain for words related to neglecting plants.',
    'value' : '6.2.4.5 Neglect plants'
  },
  {
    'guid' : 'c2630384-2f72-4a96-baed-3fff03383362',
    'id' : '6.2.5',
    'code' : '6.2.5',
    'abbr' : '6.2.5',
    'name' : { 'en': 'Harvest' },
    'description' : 'Use this domain for words related to harvesting crops. If there is an important crop and there are a lot of words that refer to harvesting it, set up a special domain for it, such as "Harvest rice" or "Harvest coconuts". If there is more than one such crop, set up a separate domain for each of them.',
    'value' : '6.2.5 Harvest'
  },
  {
    'guid' : '0e5a6bd0-470f-4231-9f57-a73b725807f4',
    'id' : '6.2.5.1',
    'code' : '6.2.5.1',
    'abbr' : '6.2.5.1',
    'name' : { 'en': 'First fruits' },
    'description' : 'Use this domain for words referring to the first fruits or crops to be harvested.',
    'value' : '6.2.5.1 First fruits'
  },
  {
    'guid' : '35faefb3-7498-4735-9b05-e7035dd368fc',
    'id' : '6.2.5.2',
    'code' : '6.2.5.2',
    'abbr' : '6.2.5.2',
    'name' : { 'en': 'Crop failure' },
    'description' : 'Use this domain for words related to crop failure.',
    'value' : '6.2.5.2 Crop failure'
  },
  {
    'guid' : 'a8bfd196-8b54-4084-b11a-fe6e8bc5da4c',
    'id' : '6.2.5.3',
    'code' : '6.2.5.3',
    'abbr' : '6.2.5.3',
    'name' : { 'en': 'Gather wild plants' },
    'description' : 'Use this domain for words related to gathering wild plants. In a hunter-gatherer culture this domain might need to be extensively developed.',
    'value' : '6.2.5.3 Gather wild plants'
  },
  {
    'guid' : '4bedae6a-1df4-40e5-8a2f-ab0a1f41997e',
    'id' : '6.2.5.4',
    'code' : '6.2.5.4',
    'abbr' : '6.2.5.4',
    'name' : { 'en': 'Plant product' },
    'description' : 'Use this domain for words referring to materials and substances that are taken from plants and used for various purposes. It is necessary to think through various types of plants to think of what materials are taken from each.',
    'value' : '6.2.5.4 Plant product'
  },
  {
    'guid' : '8080c7ed-e69a-4a8a-bd8d-bf3447b13630',
    'id' : '6.2.6',
    'code' : '6.2.6',
    'abbr' : '6.2.6',
    'name' : { 'en': 'Process harvest' },
    'description' : 'Use this domain for words related to processing the harvest.',
    'value' : '6.2.6 Process harvest'
  },
  {
    'guid' : '8fb9a1a6-844d-45a5-86c7-977d3e420149',
    'id' : '6.2.6.1',
    'code' : '6.2.6.1',
    'abbr' : '6.2.6.1',
    'name' : { 'en': 'Winnow grain' },
    'description' : 'Use this domain for words related to winnowing grain--to separate the chaff from the grain.',
    'value' : '6.2.6.1 Winnow grain'
  },
  {
    'guid' : '69d0a37d-f5b3-48a5-9486-5654d37b030b',
    'id' : '6.2.6.2',
    'code' : '6.2.6.2',
    'abbr' : '6.2.6.2',
    'name' : { 'en': 'Mill grain' },
    'description' : 'Use this domain for words related to milling grain.',
    'value' : '6.2.6.2 Mill grain'
  },
  {
    'guid' : '47ed6c39-b728-4ae7-be7c-c45c714c3153',
    'id' : '6.2.6.3',
    'code' : '6.2.6.3',
    'abbr' : '6.2.6.3',
    'name' : { 'en': 'Thresh' },
    'description' : 'Use this domain for words related to threshing grain.',
    'value' : '6.2.6.3 Thresh'
  },
  {
    'guid' : 'ceedce41-cdef-4766-9c5e-8ff5608c5464',
    'id' : '6.2.6.4',
    'code' : '6.2.6.4',
    'abbr' : '6.2.6.4',
    'name' : { 'en': 'Store the harvest' },
    'description' : 'Use this domain for words related to storing the harvest.',
    'value' : '6.2.6.4 Store the harvest'
  },
  {
    'guid' : 'ff9dfc70-526d-405e-b613-5a2a21c1b2d8',
    'id' : '6.2.7',
    'code' : '6.2.7',
    'abbr' : '6.2.7',
    'name' : { 'en': 'Farm worker' },
    'description' : 'Use this domain for words related to farm workers.',
    'value' : '6.2.7 Farm worker'
  },
  {
    'guid' : '0d972590-5947-4983-a092-443697baec24',
    'id' : '6.2.8',
    'code' : '6.2.8',
    'abbr' : '6.2.8',
    'name' : { 'en': 'Agricultural tool' },
    'description' : 'Use this domain for words related to agricultural tools.',
    'value' : '6.2.8 Agricultural tool'
  },
  {
    'guid' : 'c7990233-ef2e-4ea6-8d1e-ccf56e540394',
    'id' : '6.2.9',
    'code' : '6.2.9',
    'abbr' : '6.2.9',
    'name' : { 'en': 'Farmland' },
    'description' : 'Use this domain for words related to farmland.',
    'value' : '6.2.9 Farmland'
  },
  {
    'guid' : '79b580ff-64a7-445b-abcb-b49b9093779c',
    'id' : '6.3',
    'code' : '6.3',
    'abbr' : '6.3',
    'name' : { 'en': 'Animal husbandry' },
    'description' : 'Use this domain for words related to animal husbandry--working with animals.',
    'value' : '6.3 Animal husbandry'
  },
  {
    'guid' : '8c28c640-db5b-43e2-a1e6-b0e381962129',
    'id' : '6.3.1',
    'code' : '6.3.1',
    'abbr' : '6.3.1',
    'name' : { 'en': 'Domesticated animal' },
    'description' : 'Use this domain for words related to domesticated animals. Add extra domains for specific domesticated animals in your culture such as elephants in east Asia, camels in the Middle East, and llamas in South America.',
    'value' : '6.3.1 Domesticated animal'
  },
  {
    'guid' : '32fc19fd-a04e-4b69-9442-f7d57348ec55',
    'id' : '6.3.1.1',
    'code' : '6.3.1.1',
    'abbr' : '6.3.1.1',
    'name' : { 'en': 'Cattle' },
    'description' : 'Use this domain for words related to cattle and the care of cattle.',
    'value' : '6.3.1.1 Cattle'
  },
  {
    'guid' : '26b97047-edb1-44e9-8c7b-463de9cfbe78',
    'id' : '6.3.1.2',
    'code' : '6.3.1.2',
    'abbr' : '6.3.1.2',
    'name' : { 'en': 'Sheep' },
    'description' : 'Use this domain for words related to sheep.',
    'value' : '6.3.1.2 Sheep'
  },
  {
    'guid' : 'c36131c3-b5e1-4aac-a7b5-7c9cfa1e8f74',
    'id' : '6.3.1.3',
    'code' : '6.3.1.3',
    'abbr' : '6.3.1.3',
    'name' : { 'en': 'Goat' },
    'description' : 'Use this domain for words related to goats.',
    'value' : '6.3.1.3 Goat'
  },
  {
    'guid' : 'c888c7ac-8cf4-49d2-a33e-20d19d84c47b',
    'id' : '6.3.1.4',
    'code' : '6.3.1.4',
    'abbr' : '6.3.1.4',
    'name' : { 'en': 'Pig' },
    'description' : 'Use this domain for words related to pigs.',
    'value' : '6.3.1.4 Pig'
  },
  {
    'guid' : '10a82711-8829-461a-b172-fc8fff3d555c',
    'id' : '6.3.1.5',
    'code' : '6.3.1.5',
    'abbr' : '6.3.1.5',
    'name' : { 'en': 'Dog' },
    'description' : 'Use this domain for words related to dogs.',
    'value' : '6.3.1.5 Dog'
  },
  {
    'guid' : '49c878dd-277f-4bc9-b8ad-9ba192709108',
    'id' : '6.3.1.6',
    'code' : '6.3.1.6',
    'abbr' : '6.3.1.6',
    'name' : { 'en': 'Cat' },
    'description' : 'Use this domain for words related to cats.',
    'value' : '6.3.1.6 Cat'
  },
  {
    'guid' : 'e545baab-581d-4af8-81f2-5a884e272349',
    'id' : '6.3.1.7',
    'code' : '6.3.1.7',
    'abbr' : '6.3.1.7',
    'name' : { 'en': 'Beast of burden' },
    'description' : 'Use this domain for words related to animals that are used as a beast of burden--either to ride, to carry loads, or to pull vehicles. The questions refer to horses, but can be applied to any animal.',
    'value' : '6.3.1.7 Beast of burden'
  },
  {
    'guid' : '167a5bae-f06f-424c-bfcb-ec547a076c8d',
    'id' : '6.3.2',
    'code' : '6.3.2',
    'abbr' : '6.3.2',
    'name' : { 'en': 'Tend herds in fields' },
    'description' : 'Use this domain for words related to tending a herd in the fields.',
    'value' : '6.3.2 Tend herds in fields'
  },
  {
    'guid' : 'd85391fa-680a-4715-81ac-c0835acac8c5',
    'id' : '6.3.3',
    'code' : '6.3.3',
    'abbr' : '6.3.3',
    'name' : { 'en': 'Milk' },
    'description' : 'Use this domain for words related to milking an animal.',
    'value' : '6.3.3 Milk'
  },
  {
    'guid' : '71a2cc77-f968-4341-84c1-6c16d007a093',
    'id' : '6.3.4',
    'code' : '6.3.4',
    'abbr' : '6.3.4',
    'name' : { 'en': 'Butcher, slaughter' },
    'description' : 'Use this domain for words referring to killing animals and cutting them up for food.',
    'value' : '6.3.4 Butcher, slaughter'
  },
  {
    'guid' : 'c880c81f-65dc-4d93-8c39-22920fdbe4c7',
    'id' : '6.3.5',
    'code' : '6.3.5',
    'abbr' : '6.3.5',
    'name' : { 'en': 'Wool production' },
    'description' : 'Use this domain for words related to wool production--cutting the hair off of a sheep.',
    'value' : '6.3.5 Wool production'
  },
  {
    'guid' : 'b2bb077a-92c8-4e54-8dfa-c89efd60b82d',
    'id' : '6.3.6',
    'code' : '6.3.6',
    'abbr' : '6.3.6',
    'name' : { 'en': 'Poultry raising' },
    'description' : 'Use this domain for words related to raising birds.',
    'value' : '6.3.6 Poultry raising'
  },
  {
    'guid' : 'd7da5318-dccf-477f-967d-1e3f6a421860',
    'id' : '6.3.6.1',
    'code' : '6.3.6.1',
    'abbr' : '6.3.6.1',
    'name' : { 'en': 'Chicken' },
    'description' : 'Use this domain for words related to chickens.',
    'value' : '6.3.6.1 Chicken'
  },
  {
    'guid' : '040e4b3e-2f36-430a-ab09-1917f96a09de',
    'id' : '6.3.7',
    'code' : '6.3.7',
    'abbr' : '6.3.7',
    'name' : { 'en': 'Animal products' },
    'description' : 'Use this domain for words related to animal products.',
    'value' : '6.3.7 Animal products'
  },
  {
    'guid' : '71dbaf5f-2afa-4282-b9b8-8a50d8c8e5e9',
    'id' : '6.3.8',
    'code' : '6.3.8',
    'abbr' : '6.3.8',
    'name' : { 'en': 'Veterinary science' },
    'description' : 'Use this domain for words related to treating animal diseases.',
    'value' : '6.3.8 Veterinary science'
  },
  {
    'guid' : 'acddd447-bf8a-4d74-8501-7defb0525cc0',
    'id' : '6.3.8.1',
    'code' : '6.3.8.1',
    'abbr' : '6.3.8.1',
    'name' : { 'en': 'Animal diseases' },
    'description' : 'Use this domain for words related to animal diseases.',
    'value' : '6.3.8.1 Animal diseases'
  },
  {
    'guid' : '4f80a620-30db-4529-94e8-f0cd9d0b0e96',
    'id' : '6.3.8.2',
    'code' : '6.3.8.2',
    'abbr' : '6.3.8.2',
    'name' : { 'en': 'Castrate animal' },
    'description' : 'Use this domain for words related to castrating animals. There are sometimes specific terms for castrated animals, such as "steer--a male cow that has been castrated before maturity".',
    'value' : '6.3.8.2 Castrate animal'
  },
  {
    'guid' : '9e6d7c69-788c-4d40-8584-32f185e91932',
    'id' : '6.4',
    'code' : '6.4',
    'abbr' : '6.4',
    'name' : { 'en': 'Hunt and fish' },
    'description' : 'Use this domain for words related to hunting and fishing--catching and killing wild animals.',
    'value' : '6.4 Hunt and fish'
  },
  {
    'guid' : 'a1ce19c4-d12c-46da-a718-6d03949b3db1',
    'id' : '6.4.1',
    'code' : '6.4.1',
    'abbr' : '6.4.1',
    'name' : { 'en': 'Hunt' },
    'description' : 'Use this domain for words related to hunting wild animals.',
    'value' : '6.4.1 Hunt'
  },
  {
    'guid' : '62b40326-f74c-4d80-9b1c-4dae0fc07026',
    'id' : '6.4.1.1',
    'code' : '6.4.1.1',
    'abbr' : '6.4.1.1',
    'name' : { 'en': 'Track an animal' },
    'description' : 'Use this domain for words related to tracking an animal.',
    'value' : '6.4.1.1 Track an animal'
  },
  {
    'guid' : '196f81d0-6a1a-4cc0-936a-367423ff485c',
    'id' : '6.4.2',
    'code' : '6.4.2',
    'abbr' : '6.4.2',
    'name' : { 'en': 'Trap' },
    'description' : 'Use this domain for words related to trapping an animal.',
    'value' : '6.4.2 Trap'
  },
  {
    'guid' : '4e0992cd-c04c-4b55-beab-6b0a3c98a994',
    'id' : '6.4.3',
    'code' : '6.4.3',
    'abbr' : '6.4.3',
    'name' : { 'en': 'Hunting birds' },
    'description' : 'Use this domain for words related to hunting birds.',
    'value' : '6.4.3 Hunting birds'
  },
  {
    'guid' : 'b0e2635e-47c4-4995-942b-07f6635faf6f',
    'id' : '6.4.4',
    'code' : '6.4.4',
    'abbr' : '6.4.4',
    'name' : { 'en': 'Beekeeping' },
    'description' : 'Use this domain for words related to keeping bees.',
    'value' : '6.4.4 Beekeeping'
  },
  {
    'guid' : 'ea46de30-a1a9-4828-84a8-9165f61f8b20',
    'id' : '6.4.5',
    'code' : '6.4.5',
    'abbr' : '6.4.5',
    'name' : { 'en': 'Fishing' },
    'description' : 'Use this domain for words related to catching fish.',
    'value' : '6.4.5 Fishing'
  },
  {
    'guid' : '573bf23a-3fde-4552-9263-62b7c71cad02',
    'id' : '6.4.5.1',
    'code' : '6.4.5.1',
    'abbr' : '6.4.5.1',
    'name' : { 'en': 'Fish with net' },
    'description' : 'Use this domain for words related to fishing with a net.',
    'value' : '6.4.5.1 Fish with net'
  },
  {
    'guid' : 'd7e0ed88-6d5a-44cc-a0fe-070a5aab3e60',
    'id' : '6.4.5.2',
    'code' : '6.4.5.2',
    'abbr' : '6.4.5.2',
    'name' : { 'en': 'Fish with hooks' },
    'description' : 'Use this domain for words related to fishing with a hook and line.',
    'value' : '6.4.5.2 Fish with hooks'
  },
  {
    'guid' : 'eb07e333-38d2-4ddb-9bc9-5990403600b4',
    'id' : '6.4.5.3',
    'code' : '6.4.5.3',
    'abbr' : '6.4.5.3',
    'name' : { 'en': 'Fishing equipment' },
    'description' : 'Use this domain for words related to fishing equipment.',
    'value' : '6.4.5.3 Fishing equipment'
  },
  {
    'guid' : '7c234ccc-0dbe-42b0-a377-db99ebd2b51e',
    'id' : '6.4.6',
    'code' : '6.4.6',
    'abbr' : '6.4.6',
    'name' : { 'en': 'Things done to animals' },
    'description' : 'Use this domain for words related to things done to animals.',
    'value' : '6.4.6 Things done to animals'
  },
  {
    'guid' : 'cde96694-79e5-44af-8d38-d988d1938e5f',
    'id' : '6.5',
    'code' : '6.5',
    'abbr' : '6.5',
    'name' : { 'en': 'Working with buildings' },
    'description' : 'Use this domain for words related to working with buildings.',
    'value' : '6.5 Working with buildings'
  },
  {
    'guid' : 'dc177f3c-d0fd-4232-adf1-a77b339cdbb2',
    'id' : '6.5.1',
    'code' : '6.5.1',
    'abbr' : '6.5.1',
    'name' : { 'en': 'Building' },
    'description' : 'Use this domain for words related to buildings and other large structures that people build.',
    'value' : '6.5.1 Building'
  },
  {
    'guid' : '191ca5a5-0a67-426e-adfc-6fdf7c2aaa2c',
    'id' : '6.5.1.1',
    'code' : '6.5.1.1',
    'abbr' : '6.5.1.1',
    'name' : { 'en': 'House' },
    'description' : 'Use this domain for general words referring to a house where people live.',
    'value' : '6.5.1.1 House'
  },
  {
    'guid' : 'c8e3c39c-d895-4e42-8e1e-1574137ba016',
    'id' : '6.5.1.2',
    'code' : '6.5.1.2',
    'abbr' : '6.5.1.2',
    'name' : { 'en': 'Types of houses' },
    'description' : 'Use this domain for words referring to types of houses.',
    'value' : '6.5.1.2 Types of houses'
  },
  {
    'guid' : '7981a8e1-cf0b-44a0-9de4-12160b2f201d',
    'id' : '6.5.1.3',
    'code' : '6.5.1.3',
    'abbr' : '6.5.1.3',
    'name' : { 'en': 'Land, property' },
    'description' : 'Use this domain for words related to land that a person owns or the land on which a house is built.',
    'value' : '6.5.1.3 Land, property'
  },
  {
    'guid' : 'c1ceebe1-1274-40c1-a932-696265d9d412',
    'id' : '6.5.1.4',
    'code' : '6.5.1.4',
    'abbr' : '6.5.1.4',
    'name' : { 'en': 'Yard' },
    'description' : 'Use this domain for words referring to the outside of a house, the area around a house, a barrier separating one house from another, and the entryway to the area around a house.',
    'value' : '6.5.1.4 Yard'
  },
  {
    'guid' : '7a8cb8d3-797d-478d-b7dd-265a1eedc0c1',
    'id' : '6.5.1.5',
    'code' : '6.5.1.5',
    'abbr' : '6.5.1.5',
    'name' : { 'en': 'Fence, wall' },
    'description' : 'Use this domain for words referring to a fence, wall, hedge, or other barrier separating one house from another, and a gate or entryway through a fence or wall.',
    'value' : '6.5.1.5 Fence, wall'
  },
  {
    'guid' : '22acd714-b11e-462a-bd8e-6ff50843c103',
    'id' : '6.5.2',
    'code' : '6.5.2',
    'abbr' : '6.5.2',
    'name' : { 'en': 'Parts of a building' },
    'description' : 'Use this domain for words referring to the parts and areas of a building.',
    'value' : '6.5.2 Parts of a building'
  },
  {
    'guid' : '50903b35-5606-4727-8474-01c06bf588da',
    'id' : '6.5.2.1',
    'code' : '6.5.2.1',
    'abbr' : '6.5.2.1',
    'name' : { 'en': 'Wall' },
    'description' : 'Use this domain for words related to walls.',
    'value' : '6.5.2.1 Wall'
  },
  {
    'guid' : 'eec72226-106c-4825-b245-6e18110ee917',
    'id' : '6.5.2.2',
    'code' : '6.5.2.2',
    'abbr' : '6.5.2.2',
    'name' : { 'en': 'Roof' },
    'description' : 'Use this domain for words related to a roof.',
    'value' : '6.5.2.2 Roof'
  },
  {
    'guid' : '3a568f98-8446-4327-876b-7c5ec78d9084',
    'id' : '6.5.2.3',
    'code' : '6.5.2.3',
    'abbr' : '6.5.2.3',
    'name' : { 'en': 'Floor' },
    'description' : 'Use this domain for words related to a floor.',
    'value' : '6.5.2.3 Floor'
  },
  {
    'guid' : 'bafa274e-8bf0-4cf7-8ce7-2c28293db809',
    'id' : '6.5.2.4',
    'code' : '6.5.2.4',
    'abbr' : '6.5.2.4',
    'name' : { 'en': 'Door' },
    'description' : 'Use this domain for words related to a door.',
    'value' : '6.5.2.4 Door'
  },
  {
    'guid' : 'eeee02e1-157e-4786-ab92-80c92e5023b8',
    'id' : '6.5.2.5',
    'code' : '6.5.2.5',
    'abbr' : '6.5.2.5',
    'name' : { 'en': 'Window' },
    'description' : 'Use this domain for words related to a window.',
    'value' : '6.5.2.5 Window'
  },
  {
    'guid' : '58be5db0-c648-4522-bad0-02cd9cc15f37',
    'id' : '6.5.2.6',
    'code' : '6.5.2.6',
    'abbr' : '6.5.2.6',
    'name' : { 'en': 'Foundation' },
    'description' : 'Use this domain for words related to the foundation of a building.',
    'value' : '6.5.2.6 Foundation'
  },
  {
    'guid' : 'd2b61570-af54-44f3-846e-6d7ec9d3737f',
    'id' : '6.5.2.7',
    'code' : '6.5.2.7',
    'abbr' : '6.5.2.7',
    'name' : { 'en': 'Room' },
    'description' : 'Use this domain for words related to the rooms of a building.',
    'value' : '6.5.2.7 Room'
  },
  {
    'guid' : '84a8d541-9571-4ce9-abea-bd9cccb8dbf0',
    'id' : '6.5.2.8',
    'code' : '6.5.2.8',
    'abbr' : '6.5.2.8',
    'name' : { 'en': 'Floor, story' },
    'description' : 'Use this domain for words related to the levels of a building.',
    'value' : '6.5.2.8 Floor, story'
  },
  {
    'guid' : '97e1aba5-2ac1-44a3-8f18-59b2347a54a1',
    'id' : '6.5.3',
    'code' : '6.5.3',
    'abbr' : '6.5.3',
    'name' : { 'en': 'Building materials' },
    'description' : 'Use this domain for words related to the materials used to make a building.',
    'value' : '6.5.3 Building materials'
  },
  {
    'guid' : '4a44ac87-5ad5-44de-8170-9fd88b056010',
    'id' : '6.5.3.1',
    'code' : '6.5.3.1',
    'abbr' : '6.5.3.1',
    'name' : { 'en': 'Building equipment and maintenance' },
    'description' : 'Use this domain for words referring to the equipment and maintenance of a building.',
    'value' : '6.5.3.1 Building equipment and maintenance'
  },
  {
    'guid' : 'c6b62d63-b355-46c9-a8c7-e0a0bf112a9e',
    'id' : '6.5.4',
    'code' : '6.5.4',
    'abbr' : '6.5.4',
    'name' : { 'en': 'Infrastructure' },
    'description' : 'Use this domain for words related to infrastructure--the big things people make that many people use, such as roads, electric power lines, and water supply systems.',
    'value' : '6.5.4 Infrastructure'
  },
  {
    'guid' : '31debfe3-91da-4588-b433-21b0e14a101b',
    'id' : '6.5.4.1',
    'code' : '6.5.4.1',
    'abbr' : '6.5.4.1',
    'name' : { 'en': 'Road' },
    'description' : 'Use this domain for words related to a road.',
    'value' : '6.5.4.1 Road'
  },
  {
    'guid' : '73adcfbe-8a74-4fda-969f-616964226b9b',
    'id' : '6.5.4.2',
    'code' : '6.5.4.2',
    'abbr' : '6.5.4.2',
    'name' : { 'en': 'Boundary' },
    'description' : 'Use this domain for words related to the boundary of an area.',
    'value' : '6.5.4.2 Boundary'
  },
  {
    'guid' : '9bab95c4-9773-4894-9cb9-d7ad39378b45',
    'id' : '6.6',
    'code' : '6.6',
    'abbr' : '6.6',
    'name' : { 'en': 'Occupation' },
    'description' : 'Use this domain for words related to occupations.',
    'value' : '6.6 Occupation'
  },
  {
    'guid' : '71430132-f2ea-40fe-b3f5-b6775741cc56',
    'id' : '6.6.1',
    'code' : '6.6.1',
    'abbr' : '6.6.1',
    'name' : { 'en': 'Working with cloth' },
    'description' : 'Use this domain for words related to working with cloth.',
    'value' : '6.6.1 Working with cloth'
  },
  {
    'guid' : 'ec8e1481-827c-4554-bf50-0d3f592f3702',
    'id' : '6.6.1.1',
    'code' : '6.6.1.1',
    'abbr' : '6.6.1.1',
    'name' : { 'en': 'Cloth' },
    'description' : 'Use this domain for words related to cloth.',
    'value' : '6.6.1.1 Cloth'
  },
  {
    'guid' : '7edf9e7c-3e32-4307-b5e3-b0d704df7803',
    'id' : '6.6.1.2',
    'code' : '6.6.1.2',
    'abbr' : '6.6.1.2',
    'name' : { 'en': 'Spinning thread' },
    'description' : 'Use this domain for words related to spinning thread.',
    'value' : '6.6.1.2 Spinning thread'
  },
  {
    'guid' : 'b6e9b9c9-632b-48e7-99f7-fa53ebcb3bc5',
    'id' : '6.6.1.3',
    'code' : '6.6.1.3',
    'abbr' : '6.6.1.3',
    'name' : { 'en': 'Knitting' },
    'description' : 'Use this domain for words related to knitting.',
    'value' : '6.6.1.3 Knitting'
  },
  {
    'guid' : '77d1610f-4757-46de-b5f8-e127dc270dfd',
    'id' : '6.6.1.4',
    'code' : '6.6.1.4',
    'abbr' : '6.6.1.4',
    'name' : { 'en': 'Weaving cloth' },
    'description' : 'Use this domain for words related to weaving cloth.',
    'value' : '6.6.1.4 Weaving cloth'
  },
  {
    'guid' : '34e92ff1-32aa-49c7-b4da-d161bedc5adc',
    'id' : '6.6.2',
    'code' : '6.6.2',
    'abbr' : '6.6.2',
    'name' : { 'en': 'Working with minerals' },
    'description' : 'Use this domain for words related to working with minerals.',
    'value' : '6.6.2 Working with minerals'
  },
  {
    'guid' : 'c5996b7d-0acc-4ac7-bfa0-09b93a0eccbc',
    'id' : '6.6.2.1',
    'code' : '6.6.2.1',
    'abbr' : '6.6.2.1',
    'name' : { 'en': 'Mining' },
    'description' : 'Use this domain for words related to mining.',
    'value' : '6.6.2.1 Mining'
  },
  {
    'guid' : '993b8955-52db-4b2a-8e9a-405a155e7b60',
    'id' : '6.6.2.2',
    'code' : '6.6.2.2',
    'abbr' : '6.6.2.2',
    'name' : { 'en': 'Smelting' },
    'description' : 'Use this domain for words related to smelting--melting rocks to get metal out of them.',
    'value' : '6.6.2.2 Smelting'
  },
  {
    'guid' : '7ee3ccb5-e6cb-4028-9af5-62ae782967b5',
    'id' : '6.6.2.3',
    'code' : '6.6.2.3',
    'abbr' : '6.6.2.3',
    'name' : { 'en': 'Working with metal' },
    'description' : 'Use this domain for words related to working with metal. Answer each question below for each kind of smith.',
    'value' : '6.6.2.3 Working with metal'
  },
  {
    'guid' : '66abfbe5-e011-48de-8773-905f1b1ce215',
    'id' : '6.6.2.4',
    'code' : '6.6.2.4',
    'abbr' : '6.6.2.4',
    'name' : { 'en': 'Working with clay' },
    'description' : 'Use this domain for words related to working with clay.',
    'value' : '6.6.2.4 Working with clay'
  },
  {
    'guid' : '5a585789-2ef6-42c5-9c5a-34ff716059b7',
    'id' : '6.6.2.5',
    'code' : '6.6.2.5',
    'abbr' : '6.6.2.5',
    'name' : { 'en': 'Working with glass' },
    'description' : 'Use this domain for words related to working with glass.',
    'value' : '6.6.2.5 Working with glass'
  },
  {
    'guid' : '3f6dc9af-0c50-44d5-99f0-4aa67c668186',
    'id' : '6.6.2.6',
    'code' : '6.6.2.6',
    'abbr' : '6.6.2.6',
    'name' : { 'en': 'Working with oil and gas' },
    'description' : 'Use this domain for words related to working with oil and gas.',
    'value' : '6.6.2.6 Working with oil and gas'
  },
  {
    'guid' : '084e2568-3f54-4eab-b436-8a87fb466659',
    'id' : '6.6.2.7',
    'code' : '6.6.2.7',
    'abbr' : '6.6.2.7',
    'name' : { 'en': 'Working with stone' },
    'description' : 'Use this domain for words related to working with stone.',
    'value' : '6.6.2.7 Working with stone'
  },
  {
    'guid' : '33037a4d-3454-4c59-9a61-c5fb747f107a',
    'id' : '6.6.2.8',
    'code' : '6.6.2.8',
    'abbr' : '6.6.2.8',
    'name' : { 'en': 'Working with bricks' },
    'description' : 'Use this domain for words related to working with bricks.',
    'value' : '6.6.2.8 Working with bricks'
  },
  {
    'guid' : '8b76f4a2-9926-4c76-8d4f-563371683219',
    'id' : '6.6.2.9',
    'code' : '6.6.2.9',
    'abbr' : '6.6.2.9',
    'name' : { 'en': 'Working with chemicals' },
    'description' : 'Use this domain for words related to working with chemicals.',
    'value' : '6.6.2.9 Working with chemicals'
  },
  {
    'guid' : '1fd8a8d6-6795-4a5b-90e0-342e8b0975a1',
    'id' : '6.6.2.9.1',
    'code' : '6.6.2.9.1',
    'abbr' : '6.6.2.9.1',
    'name' : { 'en': 'Explode' },
    'description' : 'Use this domain for words related to bombs or chemicals exploding.',
    'value' : '6.6.2.9.1 Explode'
  },
  {
    'guid' : 'adde4a66-9040-47a9-91ab-327934a2e97e',
    'id' : '6.6.3',
    'code' : '6.6.3',
    'abbr' : '6.6.3',
    'name' : { 'en': 'Working with wood' },
    'description' : 'Use this domain for words related to working with wood.',
    'value' : '6.6.3 Working with wood'
  },
  {
    'guid' : '345e019f-87d2-415d-ba37-9fb85460f7e1',
    'id' : '6.6.3.1',
    'code' : '6.6.3.1',
    'abbr' : '6.6.3.1',
    'name' : { 'en': 'Lumbering' },
    'description' : 'Use this domain for words related to lumbering--cutting down trees and cutting them up.',
    'value' : '6.6.3.1 Lumbering'
  },
  {
    'guid' : '0fef044a-c822-450d-b54a-eac8621e50c2',
    'id' : '6.6.3.2',
    'code' : '6.6.3.2',
    'abbr' : '6.6.3.2',
    'name' : { 'en': 'Wood' },
    'description' : 'Use this domain for words related to wood.',
    'value' : '6.6.3.2 Wood'
  },
  {
    'guid' : 'd8389a63-8b39-4e23-8528-cd756dae2f5c',
    'id' : '6.6.3.3',
    'code' : '6.6.3.3',
    'abbr' : '6.6.3.3',
    'name' : { 'en': 'Working with paper' },
    'description' : 'Use this domain for words related to paper.',
    'value' : '6.6.3.3 Working with paper'
  },
  {
    'guid' : 'f6eb81d5-caba-4735-be6f-ae038656b555',
    'id' : '6.6.4',
    'code' : '6.6.4',
    'abbr' : '6.6.4',
    'name' : { 'en': 'Crafts' },
    'description' : 'Use this domain for words related to crafts.',
    'value' : '6.6.4 Crafts'
  },
  {
    'guid' : '0b7bfd0a-249c-45b6-9427-2c17ae00bf37',
    'id' : '6.6.4.1',
    'code' : '6.6.4.1',
    'abbr' : '6.6.4.1',
    'name' : { 'en': 'Cordage' },
    'description' : 'Use this domain for words related to working with cords and ropes.',
    'value' : '6.6.4.1 Cordage'
  },
  {
    'guid' : '019e3b64-c68a-4b19-bec5-a22f4eb88f48',
    'id' : '6.6.4.2',
    'code' : '6.6.4.2',
    'abbr' : '6.6.4.2',
    'name' : { 'en': 'Weaving baskets and mats' },
    'description' : 'Use this domain for words related to weaving baskets, mats, and other things.',
    'value' : '6.6.4.2 Weaving baskets and mats'
  },
  {
    'guid' : '7ee96f62-7c8e-4c27-a373-d7a534844612',
    'id' : '6.6.4.3',
    'code' : '6.6.4.3',
    'abbr' : '6.6.4.3',
    'name' : { 'en': 'Working with leather' },
    'description' : 'Use this domain for words related to working with leather.',
    'value' : '6.6.4.3 Working with leather'
  },
  {
    'guid' : '49471924-2458-4cb0-9430-f38cfc2fb63b',
    'id' : '6.6.4.4',
    'code' : '6.6.4.4',
    'abbr' : '6.6.4.4',
    'name' : { 'en': 'Working with bone' },
    'description' : 'Use this domain for words related to working with bone.',
    'value' : '6.6.4.4 Working with bone'
  },
  {
    'guid' : '0bc02285-8e70-442a-8d08-e04d922507c8',
    'id' : '6.6.5',
    'code' : '6.6.5',
    'abbr' : '6.6.5',
    'name' : { 'en': 'Art' },
    'description' : 'Use this domain for words related to art.',
    'value' : '6.6.5 Art'
  },
  {
    'guid' : '89ba59d4-b314-4070-83bb-f9f868bcd363',
    'id' : '6.6.5.1',
    'code' : '6.6.5.1',
    'abbr' : '6.6.5.1',
    'name' : { 'en': 'Draw, paint' },
    'description' : 'Use this domain for words related to drawing and painting pictures.',
    'value' : '6.6.5.1 Draw, paint'
  },
  {
    'guid' : '6b208fda-544c-4cba-b8cc-887b1018837f',
    'id' : '6.6.5.2',
    'code' : '6.6.5.2',
    'abbr' : '6.6.5.2',
    'name' : { 'en': 'Photography' },
    'description' : 'Use this domain for words related to photography.',
    'value' : '6.6.5.2 Photography'
  },
  {
    'guid' : '3ae3a1be-cfb5-4953-b65b-68f0c51b1d40',
    'id' : '6.6.5.3',
    'code' : '6.6.5.3',
    'abbr' : '6.6.5.3',
    'name' : { 'en': 'Sculpture' },
    'description' : 'Use this domain for words related to sculpture.',
    'value' : '6.6.5.3 Sculpture'
  },
  {
    'guid' : '0644f4da-c9fe-4239-bbe5-6efc85f98968',
    'id' : '6.6.6',
    'code' : '6.6.6',
    'abbr' : '6.6.6',
    'name' : { 'en': 'Working with land' },
    'description' : 'Use this domain for words related to working with land.',
    'value' : '6.6.6 Working with land'
  },
  {
    'guid' : '683f570a-0bfe-4a97-b55d-4319b328508b',
    'id' : '6.6.7',
    'code' : '6.6.7',
    'abbr' : '6.6.7',
    'name' : { 'en': 'Working with water' },
    'description' : 'Use this domain for words related to working with water.',
    'value' : '6.6.7 Working with water'
  },
  {
    'guid' : '515f9b40-0637-4ce3-b343-2d99de3f723b',
    'id' : '6.6.7.1',
    'code' : '6.6.7.1',
    'abbr' : '6.6.7.1',
    'name' : { 'en': 'Plumber' },
    'description' : 'Use this domain for words related to working with water pipes.',
    'value' : '6.6.7.1 Plumber'
  },
  {
    'guid' : '0205145d-23b6-4c3c-bf2d-bf866bb010e7',
    'id' : '6.6.7.2',
    'code' : '6.6.7.2',
    'abbr' : '6.6.7.2',
    'name' : { 'en': 'Conveying water' },
    'description' : 'Use this domain for words related to conveying water--moving water from one place to another place.',
    'value' : '6.6.7.2 Conveying water'
  },
  {
    'guid' : '6f83b918-dc9f-4053-90a4-a6b9e750db29',
    'id' : '6.6.7.3',
    'code' : '6.6.7.3',
    'abbr' : '6.6.7.3',
    'name' : { 'en': 'Controlling water' },
    'description' : 'Use this domain for words related to controlling the movement of water.',
    'value' : '6.6.7.3 Controlling water'
  },
  {
    'guid' : 'a6616a09-df51-4ca6-850f-733ee73c8750',
    'id' : '6.6.7.4',
    'code' : '6.6.7.4',
    'abbr' : '6.6.7.4',
    'name' : { 'en': 'Working in the sea' },
    'description' : 'Use this domain for words related to working in the sea.',
    'value' : '6.6.7.4 Working in the sea'
  },
  {
    'guid' : 'f1373316-7917-4dca-9d33-c6b520bd4034',
    'id' : '6.6.8',
    'code' : '6.6.8',
    'abbr' : '6.6.8',
    'name' : { 'en': 'Working with machines' },
    'description' : 'Use this domain for words related to working with machines.',
    'value' : '6.6.8 Working with machines'
  },
  {
    'guid' : 'b917ffec-ab7e-496a-bfe4-35c567fa0785',
    'id' : '6.6.8.1',
    'code' : '6.6.8.1',
    'abbr' : '6.6.8.1',
    'name' : { 'en': 'Working with electricity' },
    'description' : 'Use this domain for words related to working with electricity.',
    'value' : '6.6.8.1 Working with electricity'
  },
  {
    'guid' : '6342c8bf-55b5-400f-af7e-63055fe6813b',
    'id' : '6.7',
    'code' : '6.7',
    'abbr' : '6.7',
    'name' : { 'en': 'Tool' },
    'description' : 'Use this domain for general words for tools and machines. The domains in this section should be used for general tools and machines that are used in a variety of tasks. Specialized tools and machines should be classified under the specific work or activity for which they are used.',
    'value' : '6.7 Tool'
  },
  {
    'guid' : 'efe8e7b5-e008-4a3a-b3e1-23ae03a0083e',
    'id' : '6.7.1',
    'code' : '6.7.1',
    'abbr' : '6.7.1',
    'name' : { 'en': 'Cutting tool' },
    'description' : 'Use this domain for words related to cutting tools.',
    'value' : '6.7.1 Cutting tool'
  },
  {
    'guid' : 'a3bc1fbd-9f2f-4a9d-ae6e-7b02da8a05b4',
    'id' : '6.7.1.1',
    'code' : '6.7.1.1',
    'abbr' : '6.7.1.1',
    'name' : { 'en': 'Poking tool' },
    'description' : 'Use this domain for words related to poking tools--tools used to make holes in things.',
    'value' : '6.7.1.1 Poking tool'
  },
  {
    'guid' : '818e33ff-590e-4d0e-b84e-67771324a545',
    'id' : '6.7.1.2',
    'code' : '6.7.1.2',
    'abbr' : '6.7.1.2',
    'name' : { 'en': 'Digging tool' },
    'description' : 'Use this domain for words related to digging tools.',
    'value' : '6.7.1.2 Digging tool'
  },
  {
    'guid' : '65c68427-2e20-42b2-8f49-623055ea9246',
    'id' : '6.7.2',
    'code' : '6.7.2',
    'abbr' : '6.7.2',
    'name' : { 'en': 'Pounding tool' },
    'description' : 'Use this domain for words related to pounding tools.',
    'value' : '6.7.2 Pounding tool'
  },
  {
    'guid' : '62964baf-fea6-4d8e-8509-d312bff8761a',
    'id' : '6.7.3',
    'code' : '6.7.3',
    'abbr' : '6.7.3',
    'name' : { 'en': 'Carrying tool' },
    'description' : 'Use this domain for words related to carrying tools.',
    'value' : '6.7.3 Carrying tool'
  },
  {
    'guid' : 'be4ab208-1fa0-463f-9ca0-4c7e3e03aafd',
    'id' : '6.7.4',
    'code' : '6.7.4',
    'abbr' : '6.7.4',
    'name' : { 'en': 'Lifting tool' },
    'description' : 'Use this domain for words referring to tools used to lift things.',
    'value' : '6.7.4 Lifting tool'
  },
  {
    'guid' : '5cd2f365-2d84-451e-9f2d-bc2561eff909',
    'id' : '6.7.5',
    'code' : '6.7.5',
    'abbr' : '6.7.5',
    'name' : { 'en': 'Fastening tool' },
    'description' : 'Use this domain for words related to fastening tools.',
    'value' : '6.7.5 Fastening tool'
  },
  {
    'guid' : 'b2fee662-c451-4d32-aca5-a913ca0b2164',
    'id' : '6.7.6',
    'code' : '6.7.6',
    'abbr' : '6.7.6',
    'name' : { 'en': 'Holding tool' },
    'description' : 'Use this domain for words related to holding tools--tools used to grip and hold things so that they don"t move, and tools used to hold things that you can"t hold with your hand, such as very hot things.',
    'value' : '6.7.6 Holding tool'
  },
  {
    'guid' : 'eca46133-c350-4573-a349-9b7ce11b6fa8',
    'id' : '6.7.7',
    'code' : '6.7.7',
    'abbr' : '6.7.7',
    'name' : { 'en': 'Container' },
    'description' : 'Use this domain for words related to containers.',
    'value' : '6.7.7 Container'
  },
  {
    'guid' : '4d3412e3-85a0-4f81-9dad-efd6101b4945',
    'id' : '6.7.7.1',
    'code' : '6.7.7.1',
    'abbr' : '6.7.7.1',
    'name' : { 'en': 'Bag' },
    'description' : 'Use this domain for words related to bags.',
    'value' : '6.7.7.1 Bag'
  },
  {
    'guid' : '3acf5e20-b626-4f0a-a582-d386a0e30792',
    'id' : '6.7.7.2',
    'code' : '6.7.7.2',
    'abbr' : '6.7.7.2',
    'name' : { 'en': 'Sheath' },
    'description' : 'Use this domain for words related to a sheath--a container for a weapon.',
    'value' : '6.7.7.2 Sheath'
  },
  {
    'guid' : '3aec74e5-6cfd-46d2-b26f-503fad761583',
    'id' : '6.7.8',
    'code' : '6.7.8',
    'abbr' : '6.7.8',
    'name' : { 'en': 'Parts of tools' },
    'description' : 'Use this domain for words referring to parts of tools and machines. You need to think of different tools and machines, and think of each part.',
    'value' : '6.7.8 Parts of tools'
  },
  {
    'guid' : '7575d654-e528-4fe0-85eb-481b0e6654bb',
    'id' : '6.7.9',
    'code' : '6.7.9',
    'abbr' : '6.7.9',
    'name' : { 'en': 'Machine' },
    'description' : 'Use this domain for words referring to machines.',
    'value' : '6.7.9 Machine'
  },
  {
    'guid' : '2b893d04-3450-4862-b046-7df6f87272f6',
    'id' : '6.8',
    'code' : '6.8',
    'abbr' : '6.8',
    'name' : { 'en': 'Finance' },
    'description' : 'Use this domain for words related to finance.',
    'value' : '6.8 Finance'
  },
  {
    'guid' : 'ff736f67-197b-46b9-bc14-edbeb1fb3d5a',
    'id' : '6.8.1',
    'code' : '6.8.1',
    'abbr' : '6.8.1',
    'name' : { 'en': 'Have wealth' },
    'description' : 'Use this domain for words related to having wealth.',
    'value' : '6.8.1 Have wealth'
  },
  {
    'guid' : 'd9cb2e69-133d-4525-bca5-50b0f3402cbb',
    'id' : '6.8.1.1',
    'code' : '6.8.1.1',
    'abbr' : '6.8.1.1',
    'name' : { 'en': 'Own, possess' },
    'description' : 'Use this domain for words related to owning something.',
    'value' : '6.8.1.1 Own, possess'
  },
  {
    'guid' : 'c1a70060-ba04-4f16-879e-5563492aee02',
    'id' : '6.8.1.2',
    'code' : '6.8.1.2',
    'abbr' : '6.8.1.2',
    'name' : { 'en': 'Rich' },
    'description' : 'Use this domain for words related to being rich.',
    'value' : '6.8.1.2 Rich'
  },
  {
    'guid' : '45993c48-3893-4d9e-96a3-b6b1ad160538',
    'id' : '6.8.1.3',
    'code' : '6.8.1.3',
    'abbr' : '6.8.1.3',
    'name' : { 'en': 'Poor' },
    'description' : 'Use this domain for words related to being poor.',
    'value' : '6.8.1.3 Poor'
  },
  {
    'guid' : '2e2a17ba-9d81-4a3d-8af5-96c8f0e39e7e',
    'id' : '6.8.1.4',
    'code' : '6.8.1.4',
    'abbr' : '6.8.1.4',
    'name' : { 'en': 'Store wealth' },
    'description' : 'Use this domain for words related to storing wealth.',
    'value' : '6.8.1.4 Store wealth'
  },
  {
    'guid' : '3022c764-ba88-41d0-94db-393312214f4e',
    'id' : '6.8.1.5',
    'code' : '6.8.1.5',
    'abbr' : '6.8.1.5',
    'name' : { 'en': 'Possession, property' },
    'description' : 'Use this domain for words related to property--the things you own.',
    'value' : '6.8.1.5 Possession, property'
  },
  {
    'guid' : '4f22ebdb-01db-432a-9d7a-41cd44010265',
    'id' : '6.8.2',
    'code' : '6.8.2',
    'abbr' : '6.8.2',
    'name' : { 'en': 'Accumulate wealth' },
    'description' : 'Use this domain for words related to accumulating wealth.',
    'value' : '6.8.2 Accumulate wealth'
  },
  {
    'guid' : 'd1aecdba-3938-4b0c-a2e6-7dc3b7cc5cde',
    'id' : '6.8.2.1',
    'code' : '6.8.2.1',
    'abbr' : '6.8.2.1',
    'name' : { 'en': 'Produce wealth' },
    'description' : 'Use this domain for words related to producing wealth.',
    'value' : '6.8.2.1 Produce wealth'
  },
  {
    'guid' : 'b1756402-83c4-476d-8f55-010a0a10b5d9',
    'id' : '6.8.2.2',
    'code' : '6.8.2.2',
    'abbr' : '6.8.2.2',
    'name' : { 'en': 'Make profit' },
    'description' : 'Use this domain for words related to making a profit.',
    'value' : '6.8.2.2 Make profit'
  },
  {
    'guid' : '07cf5182-d090-4432-817b-037895b5cd1d',
    'id' : '6.8.2.3',
    'code' : '6.8.2.3',
    'abbr' : '6.8.2.3',
    'name' : { 'en': 'Lose wealth' },
    'description' : 'Use this domain for words related to losing wealth.',
    'value' : '6.8.2.3 Lose wealth'
  },
  {
    'guid' : '5627904e-59c7-4dd5-aeb5-c6fe0c0a0571',
    'id' : '6.8.2.4',
    'code' : '6.8.2.4',
    'abbr' : '6.8.2.4',
    'name' : { 'en': 'Frugal' },
    'description' : 'Use this domain for words related to being frugal--to not spend a lot of money.',
    'value' : '6.8.2.4 Frugal'
  },
  {
    'guid' : 'dd830047-d7f5-4010-a8ea-ae20468a0cbf',
    'id' : '6.8.2.5',
    'code' : '6.8.2.5',
    'abbr' : '6.8.2.5',
    'name' : { 'en': 'Greedy' },
    'description' : 'Use this domain for words related to being greedy.',
    'value' : '6.8.2.5 Greedy'
  },
  {
    'guid' : '044f740b-94f3-4096-aa3a-c07f5e708346',
    'id' : '6.8.2.6',
    'code' : '6.8.2.6',
    'abbr' : '6.8.2.6',
    'name' : { 'en': 'Collect' },
    'description' : 'Use this domain for words related to collecting things you find interesting. In some cultures people collect rare or valuable things that they think are attractive or interesting, such as art, stamps, coins, books, or antiques.',
    'value' : '6.8.2.6 Collect'
  },
  {
    'guid' : 'd21db541-4122-465f-9db5-4c76f5e84426',
    'id' : '6.8.2.7',
    'code' : '6.8.2.7',
    'abbr' : '6.8.2.7',
    'name' : { 'en': 'Earn' },
    'description' : 'Use this domain for words related to earning money for work that you do.',
    'value' : '6.8.2.7 Earn'
  },
  {
    'guid' : '749ad6fe-5509-4e45-b236-84ea12de102e',
    'id' : '6.8.3',
    'code' : '6.8.3',
    'abbr' : '6.8.3',
    'name' : { 'en': 'Share wealth' },
    'description' : 'Use this domain for words related to sharing wealth with others.',
    'value' : '6.8.3 Share wealth'
  },
  {
    'guid' : '85f211ae-cc16-4042-8c27-e99ff8f01f61',
    'id' : '6.8.3.1',
    'code' : '6.8.3.1',
    'abbr' : '6.8.3.1',
    'name' : { 'en': 'Give, donate' },
    'description' : 'Use this domain for words referring to giving something to someone, in which there is a transference of ownership.',
    'value' : '6.8.3.1 Give, donate'
  },
  {
    'guid' : '339ee46b-d69a-4f2e-8fba-d1b2adff763b',
    'id' : '6.8.3.2',
    'code' : '6.8.3.2',
    'abbr' : '6.8.3.2',
    'name' : { 'en': 'Generous' },
    'description' : 'Use this domain for words related to being generous.',
    'value' : '6.8.3.2 Generous'
  },
  {
    'guid' : '0105615f-0a96-4d08-ab00-ca4b4473de39',
    'id' : '6.8.3.3',
    'code' : '6.8.3.3',
    'abbr' : '6.8.3.3',
    'name' : { 'en': 'Stingy' },
    'description' : 'Use this domain for words related to being stingy.',
    'value' : '6.8.3.3 Stingy'
  },
  {
    'guid' : '6681f03b-06c4-4509-9253-e4739c9c1614',
    'id' : '6.8.3.4',
    'code' : '6.8.3.4',
    'abbr' : '6.8.3.4',
    'name' : { 'en': 'Beg' },
    'description' : 'Use this domain for words related to begging--for a poor person to ask other people to give them money, food, or other things.',
    'value' : '6.8.3.4 Beg'
  },
  {
    'guid' : '67931f1c-9a0c-4d18-9762-e553c132256c',
    'id' : '6.8.4',
    'code' : '6.8.4',
    'abbr' : '6.8.4',
    'name' : { 'en': 'Financial transaction' },
    'description' : 'Use this domain for words related to financial transactions.',
    'value' : '6.8.4 Financial transaction'
  },
  {
    'guid' : '063e0810-8e49-44ef-aa8f-bb9e63bb66dd',
    'id' : '6.8.4.1',
    'code' : '6.8.4.1',
    'abbr' : '6.8.4.1',
    'name' : { 'en': 'Buy' },
    'description' : 'Use this domain for words related to buying something.',
    'value' : '6.8.4.1 Buy'
  },
  {
    'guid' : 'a758718d-6e90-471d-acde-a637ba9ff9eb',
    'id' : '6.8.4.2',
    'code' : '6.8.4.2',
    'abbr' : '6.8.4.2',
    'name' : { 'en': 'Sell' },
    'description' : 'Use this domain for words related to selling something.',
    'value' : '6.8.4.2 Sell'
  },
  {
    'guid' : '0f7c4d2f-ed94-49ba-a91c-fba36193f35a',
    'id' : '6.8.4.3',
    'code' : '6.8.4.3',
    'abbr' : '6.8.4.3',
    'name' : { 'en': 'Price' },
    'description' : 'Use this domain for words related to the price of something--how much money you have to pay to buy something.',
    'value' : '6.8.4.3 Price'
  },
  {
    'guid' : 'a755eaba-fce9-4a8b-b9cf-b3970a49f464',
    'id' : '6.8.4.3.1',
    'code' : '6.8.4.3.1',
    'abbr' : '6.8.4.3.1',
    'name' : { 'en': 'Expensive' },
    'description' : 'Use this domain for words related to an expensive price.',
    'value' : '6.8.4.3.1 Expensive'
  },
  {
    'guid' : 'ecbdf1c5-9d7f-4446-b6a1-644a379a480b',
    'id' : '6.8.4.3.2',
    'code' : '6.8.4.3.2',
    'abbr' : '6.8.4.3.2',
    'name' : { 'en': 'Cheap' },
    'description' : 'Use this domain for words related to a cheap price.',
    'value' : '6.8.4.3.2 Cheap'
  },
  {
    'guid' : 'ffe84c4f-8c38-4b84-ac36-e79ffadbd426',
    'id' : '6.8.4.3.3',
    'code' : '6.8.4.3.3',
    'abbr' : '6.8.4.3.3',
    'name' : { 'en': 'Free of charge' },
    'description' : 'Use this domain for words related to something being free of charge--something you can have without paying for it.',
    'value' : '6.8.4.3.3 Free of charge'
  },
  {
    'guid' : '36123ffe-14d8-4198-b32c-eabd0b23e0dd',
    'id' : '6.8.4.4',
    'code' : '6.8.4.4',
    'abbr' : '6.8.4.4',
    'name' : { 'en': 'Bargain' },
    'description' : 'Use this domain for words related to bargaining over the price of something.',
    'value' : '6.8.4.4 Bargain'
  },
  {
    'guid' : '0ca05184-08b9-4dc7-a4c7-ff762380b111',
    'id' : '6.8.4.5',
    'code' : '6.8.4.5',
    'abbr' : '6.8.4.5',
    'name' : { 'en': 'Pay' },
    'description' : 'Use this domain for words related to paying money for something.',
    'value' : '6.8.4.5 Pay'
  },
  {
    'guid' : '3b69f6b6-d64a-43aa-99dc-05e34f81e07f',
    'id' : '6.8.4.6',
    'code' : '6.8.4.6',
    'abbr' : '6.8.4.6',
    'name' : { 'en': 'Hire, rent' },
    'description' : 'Use this domain for words related to hiring or renting something--to pay money so that you can use something that belongs to someone else.',
    'value' : '6.8.4.6 Hire, rent'
  },
  {
    'guid' : '57e367f4-7029-4916-a700-791db32b4745',
    'id' : '6.8.4.7',
    'code' : '6.8.4.7',
    'abbr' : '6.8.4.7',
    'name' : { 'en': 'Spend' },
    'description' : 'Use this domain for words related to spending money.',
    'value' : '6.8.4.7 Spend'
  },
  {
    'guid' : '869d0c7b-d792-45ab-bf31-fd9f6fea3107',
    'id' : '6.8.4.8',
    'code' : '6.8.4.8',
    'abbr' : '6.8.4.8',
    'name' : { 'en': 'Store, marketplace' },
    'description' : 'Use this domain for words related to a store or marketplace where things are sold.',
    'value' : '6.8.4.8 Store, marketplace'
  },
  {
    'guid' : '7e0bc050-5298-4808-af22-5e284526c652',
    'id' : '6.8.4.9',
    'code' : '6.8.4.9',
    'abbr' : '6.8.4.9',
    'name' : { 'en': 'Exchange, trade' },
    'description' : 'Use this domain for words related to exchanging or trading things.',
    'value' : '6.8.4.9 Exchange, trade'
  },
  {
    'guid' : '49ee84ff-eb2b-4ba3-b193-3018d34599c2',
    'id' : '6.8.5',
    'code' : '6.8.5',
    'abbr' : '6.8.5',
    'name' : { 'en': 'Borrow' },
    'description' : 'Use this domain for words related to borrowing something.',
    'value' : '6.8.5 Borrow'
  },
  {
    'guid' : '48380d5d-bd54-48a9-92bc-7c8a93de0567',
    'id' : '6.8.5.1',
    'code' : '6.8.5.1',
    'abbr' : '6.8.5.1',
    'name' : { 'en': 'Lend' },
    'description' : 'Use this domain for words related to lending something.',
    'value' : '6.8.5.1 Lend'
  },
  {
    'guid' : 'ec90e061-e6a0-435f-8784-7269a24c670a',
    'id' : '6.8.5.2',
    'code' : '6.8.5.2',
    'abbr' : '6.8.5.2',
    'name' : { 'en': 'Give pledge, bond' },
    'description' : 'Use this domain for words related to giving a pledge to replay a loan.',
    'value' : '6.8.5.2 Give pledge, bond'
  },
  {
    'guid' : '4eb41e40-4115-435a-934a-5d91022a29dc',
    'id' : '6.8.5.3',
    'code' : '6.8.5.3',
    'abbr' : '6.8.5.3',
    'name' : { 'en': 'Owe' },
    'description' : 'Use this domain for words related to owing money.',
    'value' : '6.8.5.3 Owe'
  },
  {
    'guid' : '1da9c4f4-8ae2-47d9-8068-ff65fa3848a9',
    'id' : '6.8.5.4',
    'code' : '6.8.5.4',
    'abbr' : '6.8.5.4',
    'name' : { 'en': 'Repay debt' },
    'description' : 'Use this domain for words related to repaying a debt.',
    'value' : '6.8.5.4 Repay debt'
  },
  {
    'guid' : '5a2355b4-c295-4b94-86da-6ac18198bae4',
    'id' : '6.8.5.5',
    'code' : '6.8.5.5',
    'abbr' : '6.8.5.5',
    'name' : { 'en': 'Credit' },
    'description' : 'Use this domain for words referring to credit--when a lending institution, such as a bank, has some of your money, so they owe you something.',
    'value' : '6.8.5.5 Credit'
  },
  {
    'guid' : '43282de6-51e1-4e52-99fc-d54e2043fb6c',
    'id' : '6.8.6',
    'code' : '6.8.6',
    'abbr' : '6.8.6',
    'name' : { 'en': 'Money' },
    'description' : 'Use this domain for words related to money.',
    'value' : '6.8.6 Money'
  },
  {
    'guid' : '0772919e-eb4c-45e3-b705-73007f5e5583',
    'id' : '6.8.6.1',
    'code' : '6.8.6.1',
    'abbr' : '6.8.6.1',
    'name' : { 'en': 'Monetary units' },
    'description' : 'Use this domain for words related to monetary units.',
    'value' : '6.8.6.1 Monetary units'
  },
  {
    'guid' : 'ed2113c8-1784-4808-ab1a-fd269f86fa99',
    'id' : '6.8.7',
    'code' : '6.8.7',
    'abbr' : '6.8.7',
    'name' : { 'en': 'Accounting' },
    'description' : 'Use this domain for words related to accounting--to keep records of money.',
    'value' : '6.8.7 Accounting'
  },
  {
    'guid' : '9a8f2f6a-a039-45dd-80d3-d1e0911907b2',
    'id' : '6.8.8',
    'code' : '6.8.8',
    'abbr' : '6.8.8',
    'name' : { 'en': 'Tax' },
    'description' : 'Use this domain for words related to tax.',
    'value' : '6.8.8 Tax'
  },
  {
    'guid' : '63b18261-faf3-4ab2-bcb4-7c3ac4d6d6ba',
    'id' : '6.8.9',
    'code' : '6.8.9',
    'abbr' : '6.8.9',
    'name' : { 'en': 'Dishonest financial practices' },
    'description' : 'Use this domain for words related to dishonest financial practices.',
    'value' : '6.8.9 Dishonest financial practices'
  },
  {
    'guid' : 'db2232d9-8b17-4920-936f-2b6249c6f7fa',
    'id' : '6.8.9.1',
    'code' : '6.8.9.1',
    'abbr' : '6.8.9.1',
    'name' : { 'en': 'Steal' },
    'description' : 'Use this domain for words related to stealing something--to take something that does not belong to you.',
    'value' : '6.8.9.1 Steal'
  },
  {
    'guid' : 'd7ae7208-7869-46e3-90c1-676342c3d7af',
    'id' : '6.8.9.2',
    'code' : '6.8.9.2',
    'abbr' : '6.8.9.2',
    'name' : { 'en': 'Cheat' },
    'description' : 'Use this domain for words related to cheating someone.',
    'value' : '6.8.9.2 Cheat'
  },
  {
    'guid' : 'a8ae0ee7-56ca-4bdf-bd9a-c56da3ff9254',
    'id' : '6.8.9.3',
    'code' : '6.8.9.3',
    'abbr' : '6.8.9.3',
    'name' : { 'en': 'Extort money' },
    'description' : 'Use this domain for words related to extorting money--forcing someone to give you money on a regular basis by threatening them with some harm.',
    'value' : '6.8.9.3 Extort money'
  },
  {
    'guid' : 'b4fc91fe-68f7-45a6-8863-75bba1029bef',
    'id' : '6.8.9.4',
    'code' : '6.8.9.4',
    'abbr' : '6.8.9.4',
    'name' : { 'en': 'Take by force' },
    'description' : 'Use this domain for words related to taking something by force.',
    'value' : '6.8.9.4 Take by force'
  },
  {
    'guid' : '892b66f4-5dfd-4451-a491-4c4fd2179081',
    'id' : '6.8.9.5',
    'code' : '6.8.9.5',
    'abbr' : '6.8.9.5',
    'name' : { 'en': 'Bribe' },
    'description' : 'Use this domain for money given to a person to do something bad.',
    'value' : '6.8.9.5 Bribe'
  },
  {
    'guid' : 'a36b3354-8598-4228-b779-c7c922b1e61d',
    'id' : '6.8.9.6',
    'code' : '6.8.9.6',
    'abbr' : '6.8.9.6',
    'name' : { 'en': 'Smuggle' },
    'description' : 'Use this domain for words related to smuggling--taking something secretly into a country, something which is illegal or without paying duty.',
    'value' : '6.8.9.6 Smuggle'
  },
  {
    'guid' : 'f726d9bb-ae80-4c01-bdef-b600cb27736e',
    'id' : '6.9',
    'code' : '6.9',
    'abbr' : '6.9',
    'name' : { 'en': 'Business organization' },
    'description' : 'Use this domain for words related to business organization.',
    'value' : '6.9 Business organization'
  },
  {
    'guid' : '0b98fb79-222f-418c-8107-5d4e791d329c',
    'id' : '6.9.1',
    'code' : '6.9.1',
    'abbr' : '6.9.1',
    'name' : { 'en': 'Management' },
    'description' : 'Use this domain for words related to managing something.',
    'value' : '6.9.1 Management'
  },
  {
    'guid' : '5c770098-2c91-4a9c-bfa0-a7ffaa871a7f',
    'id' : '6.9.2',
    'code' : '6.9.2',
    'abbr' : '6.9.2',
    'name' : { 'en': 'Work for someone' },
    'description' : 'Use this domain for words related to working for someone.',
    'value' : '6.9.2 Work for someone'
  },
  {
    'guid' : 'b33da469-fefa-44f3-b35c-d70411bfe7e1',
    'id' : '6.9.3',
    'code' : '6.9.3',
    'abbr' : '6.9.3',
    'name' : { 'en': 'Marketing' },
    'description' : 'Use this domain for words related to the promotion of trade and sales.',
    'value' : '6.9.3 Marketing'
  },
  {
    'guid' : 'cd436263-30a3-498c-93f6-3d5682f7f7c0',
    'id' : '6.9.4',
    'code' : '6.9.4',
    'abbr' : '6.9.4',
    'name' : { 'en': 'Commerce' },
    'description' : 'Use this domain for words related to commerce--taking something to a place and trying to sell it there.',
    'value' : '6.9.4 Commerce'
  },
  {
    'guid' : '73726931-ef12-4d39-a76e-7742f4b7c9cd',
    'id' : '6.9.5',
    'code' : '6.9.5',
    'abbr' : '6.9.5',
    'name' : { 'en': 'Economics' },
    'description' : 'Use this domain for words related to economics--the study of the money, trade, and industry of a country.',
    'value' : '6.9.5 Economics'
  },
  {
    'guid' : 'a197dbfb-20e4-40c2-9c76-6abbeb1a9b12',
    'id' : '6.9.6',
    'code' : '6.9.6',
    'abbr' : '6.9.6',
    'name' : { 'en': 'Insurance' },
    'description' : 'Use this domain for words related to insurance.',
    'value' : '6.9.6 Insurance'
  },
  {
    'guid' : 'd60faf11-cc6e-48db-8a13-82f86d78ab00',
    'id' : '7',
    'code' : '7',
    'abbr' : '7',
    'name' : { 'en': 'Physical actions' },
    'description' : 'Use this domain for general words for physical actions--moving yourself, moving things, and changing things.',
    'value' : '7 Physical actions'
  },
  {
    'guid' : '9f3b4cab-dc8a-430e-88f3-d3002df64fb8',
    'id' : '7.1',
    'code' : '7.1',
    'abbr' : '7.1',
    'name' : { 'en': 'Posture' },
    'description' : 'Use this domain for general words indicating the posture or stance of a person"s body. Use the domains in this section for specific words for postures and for moving parts of the body. Many of these words have two meanings. One meaning is stative, indicating that the body is in a particular posture, but not moving. The other meaning is active, indicating that the person is moving his body into a particular posture. Some of these words can be used of animals or even things, if the things are perceived as being similar in posture to that of a human body.',
    'value' : '7.1 Posture'
  },
  {
    'guid' : '8841af5e-74e6-4c5d-a313-ba9aa7fdb78b',
    'id' : '7.1.1',
    'code' : '7.1.1',
    'abbr' : '7.1.1',
    'name' : { 'en': 'Stand' },
    'description' : 'Use this domain for words related to standing. The words in this domain may include whether the person is standing on one foot or two, whether the person"s feet are together or placed apart, whether something is between the person"s feet, how fast the person stands up.',
    'value' : '7.1.1 Stand'
  },
  {
    'guid' : '975d0109-1bba-4b0e-85b8-2c6b51c8e074',
    'id' : '7.1.2',
    'code' : '7.1.2',
    'abbr' : '7.1.2',
    'name' : { 'en': 'Sit' },
    'description' : 'Use this domain for words referring to sitting and squatting.',
    'value' : '7.1.2 Sit'
  },
  {
    'guid' : '0aae0254-dc06-4906-8ecf-2d8450fb83f1',
    'id' : '7.1.3',
    'code' : '7.1.3',
    'abbr' : '7.1.3',
    'name' : { 'en': 'Lie down' },
    'description' : 'Use this domain for words referring to lying down.',
    'value' : '7.1.3 Lie down'
  },
  {
    'guid' : '3a28ab73-2847-44a4-97ed-7129269f1366',
    'id' : '7.1.4',
    'code' : '7.1.4',
    'abbr' : '7.1.4',
    'name' : { 'en': 'Kneel' },
    'description' : 'Use this domain for words referring to kneeling.',
    'value' : '7.1.4 Kneel'
  },
  {
    'guid' : 'c3f20ce7-d30e-40fd-af8a-713a65c46cd0',
    'id' : '7.1.5',
    'code' : '7.1.5',
    'abbr' : '7.1.5',
    'name' : { 'en': 'Bow' },
    'description' : 'Use this domain for words referring to bowing to someone--to face someone and lower your head or bend at the waist so that your upper body is lowered. The words in this domain should all be symbolic acts, that is, the culture assigns a meaning and significance to the physical action. Some cultures define it as a greeting, as an act of respect, or as an act of submission. For instance in ancient middle eastern culture bowing was an act of submission done in the presence of the king. Failure to bow was an act of rebellion punishable by death. In far eastern culture it is a greeting. In African culture it is an act of respect or done to welcome a visitor. Cultures define how a person is to bow, for instance how they are to hold their hands. A person may bow while standing, or may kneel, or even prostrate himself flat on his face. Whether both people bow is also significant. The depth of the bow can have significance. In Japan the person of lower social rank must bow deeper. In European culture a performer bows to acknowledge applause.',
    'value' : '7.1.5 Bow'
  },
  {
    'guid' : '566be8c1-3e42-4f8b-87eb-e70e8c13c6f8',
    'id' : '7.1.6',
    'code' : '7.1.6',
    'abbr' : '7.1.6',
    'name' : { 'en': 'Lean' },
    'description' : 'Use this domain for words describing a person who is leaning.',
    'value' : '7.1.6 Lean'
  },
  {
    'guid' : '85b9908f-4f57-4baa-9ed2-bc4705b12e72',
    'id' : '7.1.7',
    'code' : '7.1.7',
    'abbr' : '7.1.7',
    'name' : { 'en': 'Straight posture' },
    'description' : 'Use this domain for words referring to straight posture--holding your body straight, stiff, or erect, rather than relaxed.',
    'value' : '7.1.7 Straight posture'
  },
  {
    'guid' : '8e65904c-f9e9-4e12-b430-c1ddc540f1ae',
    'id' : '7.1.7.1',
    'code' : '7.1.7.1',
    'abbr' : '7.1.7.1',
    'name' : { 'en': 'Relaxed posture' },
    'description' : 'Use this domain for words describing a relaxed posture.',
    'value' : '7.1.7.1 Relaxed posture'
  },
  {
    'guid' : '85dfc6a3-6c70-41f2-a869-32e2fb3c40ee',
    'id' : '7.1.8',
    'code' : '7.1.8',
    'abbr' : '7.1.8',
    'name' : { 'en': 'Bend down' },
    'description' : 'Use this domain for words referring to bending down--to bend your body so that it is closer to the ground (in order to see something, pick up something, or hide).',
    'value' : '7.1.8 Bend down'
  },
  {
    'guid' : '86e5c062-d90f-476c-a363-264adfafedfa',
    'id' : '7.1.9',
    'code' : '7.1.9',
    'abbr' : '7.1.9',
    'name' : { 'en': 'Move a part of the body' },
    'description' : 'Use this domain for words referring to moving various parts of the body.',
    'value' : '7.1.9 Move a part of the body'
  },
  {
    'guid' : 'c7c85346-158d-4881-839d-9a6a8e47209b',
    'id' : '7.2',
    'code' : '7.2',
    'abbr' : '7.2',
    'name' : { 'en': 'Move' },
    'description' : 'Use this domain for intransitive verbs of movement referring to moving your whole body to a different location. There are many components of meaning involved in verbs of movement. They can include a source location (from), a goal location (to), a path (along), manner (walk, run), speed (race, crawl), direction\n(forward, back, side, up, down, toward, away from), shape of the path (straight, curve, circle, angle), relationship to some object or objects (on, hang, against, between, across, through, into, out of, onto, off of, around), proximity (near, far), multiple movements (back and forth, return, bounce), inclusion with other objects or parts of objects (join, leave, collect, disperse, unite, separate), volition (go versus drift), intransitive versus transitive (move versus move something). Some verbs of movement do not involve moving from one place to another, but express bodily movement.',
    'value' : '7.2 Move'
  },
  {
    'guid' : '5cc64831-c14a-4dbe-beba-214e725ad041',
    'id' : '7.2.1',
    'code' : '7.2.1',
    'abbr' : '7.2.1',
    'name' : { 'en': 'Manner of movement' },
    'description' : 'Use this domain for general words referring to the way in which a person moves.',
    'value' : '7.2.1 Manner of movement'
  },
  {
    'guid' : '643cc712-b3b3-42d0-971e-7a4c8a6cbf1e',
    'id' : '7.2.1.1',
    'code' : '7.2.1.1',
    'abbr' : '7.2.1.1',
    'name' : { 'en': 'Walk' },
    'description' : 'Use this domain for words related to walking--moving slowly using your legs.',
    'value' : '7.2.1.1 Walk'
  },
  {
    'guid' : 'b16de6a0-71dd-448c-9ffa-49f6646a5219',
    'id' : '7.2.1.1.1',
    'code' : '7.2.1.1.1',
    'abbr' : '7.2.1.1.1',
    'name' : { 'en': 'Run' },
    'description' : 'Use this domain for words referring to running--moving fast on your legs.',
    'value' : '7.2.1.1.1 Run'
  },
  {
    'guid' : 'a2bbf179-8d38-4d2e-84cd-0e00bb6c74f3',
    'id' : '7.2.1.1.2',
    'code' : '7.2.1.1.2',
    'abbr' : '7.2.1.1.2',
    'name' : { 'en': 'Crawl' },
    'description' : 'Use this domain for words referring to crawling--moving on your hands and knees or on your stomach.',
    'value' : '7.2.1.1.2 Crawl'
  },
  {
    'guid' : '5abd1270-261a-4980-93e5-e10eacea99ad',
    'id' : '7.2.1.1.3',
    'code' : '7.2.1.1.3',
    'abbr' : '7.2.1.1.3',
    'name' : { 'en': 'Jump' },
    'description' : 'Use this domain for words referring to jumping--moving your body off the ground by pushing hard with your legs.',
    'value' : '7.2.1.1.3 Jump'
  },
  {
    'guid' : '1017cbc3-0dfb-4930-9881-28f96784035c',
    'id' : '7.2.1.2',
    'code' : '7.2.1.2',
    'abbr' : '7.2.1.2',
    'name' : { 'en': 'Move quickly' },
    'description' : 'Use this domain for words referring to moving quickly or slowly.',
    'value' : '7.2.1.2 Move quickly'
  },
  {
    'guid' : 'e442afe1-e7cd-4ab2-b456-963e2e041a1e',
    'id' : '7.2.1.2.1',
    'code' : '7.2.1.2.1',
    'abbr' : '7.2.1.2.1',
    'name' : { 'en': 'Move slowly' },
    'description' : 'Use this domain for words referring to moving slowly.',
    'value' : '7.2.1.2.1 Move slowly'
  },
  {
    'guid' : 'deff977c-a664-4456-9d44-a5127dd2a7d1',
    'id' : '7.2.1.3',
    'code' : '7.2.1.3',
    'abbr' : '7.2.1.3',
    'name' : { 'en': 'Wander' },
    'description' : 'Use this domain for words referring to wandering--to move slowly without any purpose or goal.',
    'value' : '7.2.1.3 Wander'
  },
  {
    'guid' : '250baab9-5a31-493c-95ea-9fee8baf9fd5',
    'id' : '7.2.1.4',
    'code' : '7.2.1.4',
    'abbr' : '7.2.1.4',
    'name' : { 'en': 'Graceful' },
    'description' : 'Use this domain for words referring to moving in a graceful manner.',
    'value' : '7.2.1.4 Graceful'
  },
  {
    'guid' : 'b5aa5873-4c66-4d2d-935a-18e0ab231dbb',
    'id' : '7.2.1.4.1',
    'code' : '7.2.1.4.1',
    'abbr' : '7.2.1.4.1',
    'name' : { 'en': 'Clumsy' },
    'description' : 'Use this domain for words referring to moving in a clumsy manner.',
    'value' : '7.2.1.4.1 Clumsy'
  },
  {
    'guid' : '879e66bc-af29-4fe4-86e0-0047973a57d6',
    'id' : '7.2.1.5',
    'code' : '7.2.1.5',
    'abbr' : '7.2.1.5',
    'name' : { 'en': 'Walk with difficulty' },
    'description' : 'Use this domain for words referring to walking with some difficulty such as stumbling--to miss your step because of hitting an object such as a stone, hole, or mud; staggering--to walk unsteadily because of weakness, illness, or drunkenness; or limping--to walk unevenly or with difficulty because of injury to a foot.',
    'value' : '7.2.1.5 Walk with difficulty'
  },
  {
    'guid' : '3f535689-944a-4e9c-8f64-ec6395b7c8d7',
    'id' : '7.2.1.5.1',
    'code' : '7.2.1.5.1',
    'abbr' : '7.2.1.5.1',
    'name' : { 'en': 'Slip, slide' },
    'description' : 'Use this domain for moving across a smooth or lubricated surface.',
    'value' : '7.2.1.5.1 Slip, slide'
  },
  {
    'guid' : '985f099d-f38c-4957-907a-769d1a45ca10',
    'id' : '7.2.1.6',
    'code' : '7.2.1.6',
    'abbr' : '7.2.1.6',
    'name' : { 'en': 'Steady, unsteady' },
    'description' : 'Use this domain for words that refer to balancing yourself or something--when someone or something is able stand or move without falling.',
    'value' : '7.2.1.6 Steady, unsteady'
  },
  {
    'guid' : 'c6132280-d2aa-46f8-9e94-b087dbda09cb',
    'id' : '7.2.1.6.1',
    'code' : '7.2.1.6.1',
    'abbr' : '7.2.1.6.1',
    'name' : { 'en': 'Balance' },
    'description' : 'Use this domain for words that refer to balancing yourself or something--when someone or something is able stand or move without falling.',
    'value' : '7.2.1.6.1 Balance'
  },
  {
    'guid' : '32d5b3de-0500-4ad6-b94e-20b8001d0a91',
    'id' : '7.2.1.7',
    'code' : '7.2.1.7',
    'abbr' : '7.2.1.7',
    'name' : { 'en': 'Move noisily' },
    'description' : 'Use this domain for words referring to making a noise while moving.',
    'value' : '7.2.1.7 Move noisily'
  },
  {
    'guid' : '12fe5f6c-7f98-47ba-936a-bcd1065c2db3',
    'id' : '7.2.2',
    'code' : '7.2.2',
    'abbr' : '7.2.2',
    'name' : { 'en': 'Move in a direction' },
    'description' : 'Use the domains in this section for words referring to moving in a direction related to the orientation of the person"s body (not in relation to the position of the destination).',
    'value' : '7.2.2 Move in a direction'
  },
  {
    'guid' : '8e59041e-660b-4f3e-9a11-83217417209e',
    'id' : '7.2.2.1',
    'code' : '7.2.2.1',
    'abbr' : '7.2.2.1',
    'name' : { 'en': 'Move forward' },
    'description' : 'Use this domain for words referring to moving in a forward direction--in the same direction the person is facing.',
    'value' : '7.2.2.1 Move forward'
  },
  {
    'guid' : '6de4e99b-2b5a-493a-a208-f024d1eabdf3',
    'id' : '7.2.2.2',
    'code' : '7.2.2.2',
    'abbr' : '7.2.2.2',
    'name' : { 'en': 'Move back' },
    'description' : 'Use this domain for words referring to moving in a backward direction--the person is facing one direction, but moving toward their back--to move in the opposite direction from the direction the person is facing.',
    'value' : '7.2.2.2 Move back'
  },
  {
    'guid' : 'f4b18e9c-b465-4763-ba79-d7eed2cebcfa',
    'id' : '7.2.2.3',
    'code' : '7.2.2.3',
    'abbr' : '7.2.2.3',
    'name' : { 'en': 'Move sideways' },
    'description' : 'Use this domain for words referring to moving in a sideways direction--the person is facing one direction, but moving toward their side.',
    'value' : '7.2.2.3 Move sideways'
  },
  {
    'guid' : '9d6cbe74-93d6-41fb-b7e2-cc30adb28187',
    'id' : '7.2.2.4',
    'code' : '7.2.2.4',
    'abbr' : '7.2.2.4',
    'name' : { 'en': 'Move up' },
    'description' : 'Use this domain for words referring to moving in an upward direction or to moving to a higher place.',
    'value' : '7.2.2.4 Move up'
  },
  {
    'guid' : '16081dd6-72e5-4826-b86d-958dd82a01c0',
    'id' : '7.2.2.5',
    'code' : '7.2.2.5',
    'abbr' : '7.2.2.5',
    'name' : { 'en': 'Move down' },
    'description' : 'Use this domain for words referring to moving in a downward direction or to moving to a lower place.',
    'value' : '7.2.2.5 Move down'
  },
  {
    'guid' : 'b14db9f4-5e1d-4a2b-bec0-0d6bcb5b1a31',
    'id' : '7.2.2.5.1',
    'code' : '7.2.2.5.1',
    'abbr' : '7.2.2.5.1',
    'name' : { 'en': 'Fall' },
    'description' : 'Use this domain for words referring to something falling--for something to move down under the influence of gravity.',
    'value' : '7.2.2.5.1 Fall'
  },
  {
    'guid' : '60a5fa58-45b1-41ae-9430-5200e8bfbcb8',
    'id' : '7.2.2.6',
    'code' : '7.2.2.6',
    'abbr' : '7.2.2.6',
    'name' : { 'en': 'Turn' },
    'description' : 'Use this domain for words referring to turning and changing the direction in which one is moving.',
    'value' : '7.2.2.6 Turn'
  },
  {
    'guid' : '8bcc3b3d-dd0c-4838-a33a-b395f354c86f',
    'id' : '7.2.2.7',
    'code' : '7.2.2.7',
    'abbr' : '7.2.2.7',
    'name' : { 'en': 'Move in a circle' },
    'description' : 'Use this domain for words referring to continually changing the direction you are facing in until you are once again facing in the same direction, e.g. moving in a circle, moving around something, or rotating.',
    'value' : '7.2.2.7 Move in a circle'
  },
  {
    'guid' : '3c9fe647-2647-4f43-8bac-7facc054f7ff',
    'id' : '7.2.2.8',
    'code' : '7.2.2.8',
    'abbr' : '7.2.2.8',
    'name' : { 'en': 'Move back and forth' },
    'description' : 'Use this domain for words related to moving back and forth.',
    'value' : '7.2.2.8 Move back and forth'
  },
  {
    'guid' : '36176d59-171b-4a0a-a0f7-a8f9857536a1',
    'id' : '7.2.2.9',
    'code' : '7.2.2.9',
    'abbr' : '7.2.2.9',
    'name' : { 'en': 'Move straight without turning' },
    'description' : 'Use this domain for words referring to moving straight without changing direction.',
    'value' : '7.2.2.9 Move straight without turning'
  },
  {
    'guid' : 'fcd22c85-7ee1-4d31-8633-9dbd32344211',
    'id' : '7.2.3',
    'code' : '7.2.3',
    'abbr' : '7.2.3',
    'name' : { 'en': 'Move toward something' },
    'description' : 'Use this domain for words referring to moving toward, in the direction of, or near something or some place. The words in this domain should refer to moving in the direction of a place, but should not require that the person actually arrived. Some languages (such as German and Polish) distinguish "go on foot" from "go in a vehicle".',
    'value' : '7.2.3 Move toward something'
  },
  {
    'guid' : '5832eb32-8a90-42a7-b2bc-a9bb575b1b7c',
    'id' : '7.2.3.1',
    'code' : '7.2.3.1',
    'abbr' : '7.2.3.1',
    'name' : { 'en': 'Move away' },
    'description' : 'Use this domain for words referring to moving away from a place.',
    'value' : '7.2.3.1 Move away'
  },
  {
    'guid' : '2cc624fa-76cb-46ab-87c8-c13c6adb1c72',
    'id' : '7.2.3.2',
    'code' : '7.2.3.2',
    'abbr' : '7.2.3.2',
    'name' : { 'en': 'Go' },
    'description' : 'Use this domain for words referring to moving away from the speaker. In Australian languages many movement words are marked for direction toward or away from the speaker.',
    'value' : '7.2.3.2 Go'
  },
  {
    'guid' : 'aa8d812b-7c13-414a-ad02-a4240d2cef68',
    'id' : '7.2.3.2.1',
    'code' : '7.2.3.2.1',
    'abbr' : '7.2.3.2.1',
    'name' : { 'en': 'Come' },
    'description' : 'Use this domain for words referring to moving toward the speaker.',
    'value' : '7.2.3.2.1 Come'
  },
  {
    'guid' : '20e7d987-0d55-46d4-ab69-0b0cce2f1e24',
    'id' : '7.2.3.3',
    'code' : '7.2.3.3',
    'abbr' : '7.2.3.3',
    'name' : { 'en': 'Leave' },
    'description' : 'Use this domain for words referring to moving away from a place.',
    'value' : '7.2.3.3 Leave'
  },
  {
    'guid' : 'b08424b7-a2f1-4a0e-82d1-665249e12cfc',
    'id' : '7.2.3.3.1',
    'code' : '7.2.3.3.1',
    'abbr' : '7.2.3.3.1',
    'name' : { 'en': 'Arrive' },
    'description' : 'Use this domain for words referring to arriving at a place.',
    'value' : '7.2.3.3.1 Arrive'
  },
  {
    'guid' : '88269184-d033-454e-b750-559d5f53287c',
    'id' : '7.2.3.4',
    'code' : '7.2.3.4',
    'abbr' : '7.2.3.4',
    'name' : { 'en': 'Move in' },
    'description' : 'Use this domain for words related to moving into something, such as a house or an area.',
    'value' : '7.2.3.4 Move in'
  },
  {
    'guid' : 'd95ed463-ac64-4004-9c3b-0fce2f7639be',
    'id' : '7.2.3.4.1',
    'code' : '7.2.3.4.1',
    'abbr' : '7.2.3.4.1',
    'name' : { 'en': 'Move out' },
    'description' : 'Use this domain for words related to moving out of something, such as a house or area.',
    'value' : '7.2.3.4.1 Move out'
  },
  {
    'guid' : 'ea2d0dc5-2cdb-4686-9f48-abe65ed295a4',
    'id' : '7.2.3.5',
    'code' : '7.2.3.5',
    'abbr' : '7.2.3.5',
    'name' : { 'en': 'Move past, over, through' },
    'description' : 'Use this domain for words referring to moving past, over, or through something.',
    'value' : '7.2.3.5 Move past, over, through'
  },
  {
    'guid' : '5a021f98-2efb-4baf-9384-a553dc3df86c',
    'id' : '7.2.3.6',
    'code' : '7.2.3.6',
    'abbr' : '7.2.3.6',
    'name' : { 'en': 'Return' },
    'description' : 'Use this domain for words referring to returning to a place--to go back to a place you earlier left.',
    'value' : '7.2.3.6 Return'
  },
  {
    'guid' : '6f01b67c-33a0-4ed3-8205-f868e97a1a3a',
    'id' : '7.2.4',
    'code' : '7.2.4',
    'abbr' : '7.2.4',
    'name' : { 'en': 'Travel' },
    'description' : 'Use this domain for words referring to traveling--to move a long distance. The words in this domain may imply that the person has to sleep somewhere other than his home.',
    'value' : '7.2.4 Travel'
  },
  {
    'guid' : 'c0b7f354-a75c-41d5-a489-ae2df6364d02',
    'id' : '7.2.4.1',
    'code' : '7.2.4.1',
    'abbr' : '7.2.4.1',
    'name' : { 'en': 'Travel by land' },
    'description' : 'Use this domain for words related to traveling by land.',
    'value' : '7.2.4.1 Travel by land'
  },
  {
    'guid' : '1e102423-6167-486a-bfef-dad1c9cdf1eb',
    'id' : '7.2.4.1.1',
    'code' : '7.2.4.1.1',
    'abbr' : '7.2.4.1.1',
    'name' : { 'en': 'Vehicle' },
    'description' : 'Use this domain for things used to move.',
    'value' : '7.2.4.1.1 Vehicle'
  },
  {
    'guid' : '4d1ac5e6-dfe3-4643-b6e5-21649a01cce9',
    'id' : '7.2.4.1.2',
    'code' : '7.2.4.1.2',
    'abbr' : '7.2.4.1.2',
    'name' : { 'en': 'Railroad' },
    'description' : 'Use this domain for words related to railroads.',
    'value' : '7.2.4.1.2 Railroad'
  },
  {
    'guid' : 'f4580c19-ba9e-4f71-a46a-6f3c4b19c36c',
    'id' : '7.2.4.2',
    'code' : '7.2.4.2',
    'abbr' : '7.2.4.2',
    'name' : { 'en': 'Travel by water' },
    'description' : 'Use this domain for words related to traveling by water.',
    'value' : '7.2.4.2 Travel by water'
  },
  {
    'guid' : '4bfe53d2-fb85-4397-98a8-97d59b907064',
    'id' : '7.2.4.2.1',
    'code' : '7.2.4.2.1',
    'abbr' : '7.2.4.2.1',
    'name' : { 'en': 'Boat' },
    'description' : 'Use this domain for words related to a boat.',
    'value' : '7.2.4.2.1 Boat'
  },
  {
    'guid' : 'fffa74fb-5b5c-453f-9120-94686033d894',
    'id' : '7.2.4.2.2',
    'code' : '7.2.4.2.2',
    'abbr' : '7.2.4.2.2',
    'name' : { 'en': 'Swim' },
    'description' : 'Use this domain for words related to swimming.',
    'value' : '7.2.4.2.2 Swim'
  },
  {
    'guid' : '0bbe1739-e0b4-442e-b69c-02a0ea20d790',
    'id' : '7.2.4.2.3',
    'code' : '7.2.4.2.3',
    'abbr' : '7.2.4.2.3',
    'name' : { 'en': 'Dive' },
    'description' : 'Use this domain for words referring to moving under the water.',
    'value' : '7.2.4.2.3 Dive'
  },
  {
    'guid' : '75eb23c7-28b5-4c98-937a-1d8f371b24cf',
    'id' : '7.2.4.3',
    'code' : '7.2.4.3',
    'abbr' : '7.2.4.3',
    'name' : { 'en': 'Fly' },
    'description' : 'Use this domain for words related to traveling by air.',
    'value' : '7.2.4.3 Fly'
  },
  {
    'guid' : '161cae07-d1cb-467c-920f-62ba9039584c',
    'id' : '7.2.4.4',
    'code' : '7.2.4.4',
    'abbr' : '7.2.4.4',
    'name' : { 'en': 'Travel in space' },
    'description' : 'Use this domain for words related to traveling in space.',
    'value' : '7.2.4.4 Travel in space'
  },
  {
    'guid' : '547f1151-5816-4d89-b0bc-ece2a86c92eb',
    'id' : '7.2.4.5',
    'code' : '7.2.4.5',
    'abbr' : '7.2.4.5',
    'name' : { 'en': 'Move to a new house' },
    'description' : 'Use this domain for words related to permanently leaving your home or country, and for words related to migrating--moving every year to the same places because of the weather and food supplies.',
    'value' : '7.2.4.5 Move to a new house'
  },
  {
    'guid' : '70953222-5bc5-4fa2-a85a-01827f7bc537',
    'id' : '7.2.4.6',
    'code' : '7.2.4.6',
    'abbr' : '7.2.4.6',
    'name' : { 'en': 'Way, route' },
    'description' : 'Use this domain for words related to the way or route that you take to go somewhere.',
    'value' : '7.2.4.6 Way, route'
  },
  {
    'guid' : 'cf93b8e0-9f28-4485-b9d6-22293ccd73ce',
    'id' : '7.2.4.7',
    'code' : '7.2.4.7',
    'abbr' : '7.2.4.7',
    'name' : { 'en': 'Lose your way' },
    'description' : 'Use this domain for words related to losing your way.',
    'value' : '7.2.4.7 Lose your way'
  },
  {
    'guid' : '390ad7fc-8360-4eae-8736-3aedc15ae659',
    'id' : '7.2.4.8',
    'code' : '7.2.4.8',
    'abbr' : '7.2.4.8',
    'name' : { 'en': 'Map' },
    'description' : 'Use this domain for words related to a map--a drawing of the world or part of the world.',
    'value' : '7.2.4.8 Map'
  },
  {
    'guid' : '196bf7b1-54a1-4a78-8d10-c61585849c63',
    'id' : '7.2.5',
    'code' : '7.2.5',
    'abbr' : '7.2.5',
    'name' : { 'en': 'Accompany' },
    'description' : 'Use this domain for words referring to moving with someone.',
    'value' : '7.2.5 Accompany'
  },
  {
    'guid' : 'ecf9ebd7-f991-41df-98cd-bcf1254d5d0b',
    'id' : '7.2.5.1',
    'code' : '7.2.5.1',
    'abbr' : '7.2.5.1',
    'name' : { 'en': 'Go first' },
    'description' : 'Use this domain for words related to going first or going ahead of someone.',
    'value' : '7.2.5.1 Go first'
  },
  {
    'guid' : '1b73b2bf-9582-4f8a-822a-e0d020272c7c',
    'id' : '7.2.5.2',
    'code' : '7.2.5.2',
    'abbr' : '7.2.5.2',
    'name' : { 'en': 'Follow' },
    'description' : 'Use this domain for words related to following someone.',
    'value' : '7.2.5.2 Follow'
  },
  {
    'guid' : '902cfcbe-6e42-4e55-b2a5-9146702fc16b',
    'id' : '7.2.5.3',
    'code' : '7.2.5.3',
    'abbr' : '7.2.5.3',
    'name' : { 'en': 'Guide' },
    'description' : 'Use this domain for words related to guiding someone--to show someone where to go by going ahead of them.',
    'value' : '7.2.5.3 Guide'
  },
  {
    'guid' : '678a3319-a12b-4f92-857d-167def8ef583',
    'id' : '7.2.5.4',
    'code' : '7.2.5.4',
    'abbr' : '7.2.5.4',
    'name' : { 'en': 'Move together' },
    'description' : 'Use this domain for words referring to more than one thing moving together.',
    'value' : '7.2.5.4 Move together'
  },
  {
    'guid' : '19d54c2f-ae03-4cbc-9b7e-57292f92fbc1',
    'id' : '7.2.6',
    'code' : '7.2.6',
    'abbr' : '7.2.6',
    'name' : { 'en': 'Pursue' },
    'description' : 'Use this domain for words related to pursuing someone--to follow someone in order to catch them.',
    'value' : '7.2.6 Pursue'
  },
  {
    'guid' : '34c9408c-c3f7-49db-8bce-de7fa7da03d7',
    'id' : '7.2.6.1',
    'code' : '7.2.6.1',
    'abbr' : '7.2.6.1',
    'name' : { 'en': 'Catch, capture' },
    'description' : 'Use this domain for words related to catching someone who is trying to escape.',
    'value' : '7.2.6.1 Catch, capture'
  },
  {
    'guid' : 'd7140538-fb99-4af9-8398-8c31a1b79fb5',
    'id' : '7.2.6.2',
    'code' : '7.2.6.2',
    'abbr' : '7.2.6.2',
    'name' : { 'en': 'Prevent from moving' },
    'description' : 'Use this domain for words referring to preventing someone or something from moving.',
    'value' : '7.2.6.2 Prevent from moving'
  },
  {
    'guid' : '36e8f1df-1798-4ae6-904d-600ca6eb4145',
    'id' : '7.2.6.3',
    'code' : '7.2.6.3',
    'abbr' : '7.2.6.3',
    'name' : { 'en': 'Escape' },
    'description' : 'Use this domain for words related to escaping from danger.',
    'value' : '7.2.6.3 Escape'
  },
  {
    'guid' : '28a68cea-9128-4d5c-8542-8df38c907310',
    'id' : '7.2.6.4',
    'code' : '7.2.6.4',
    'abbr' : '7.2.6.4',
    'name' : { 'en': 'Set free' },
    'description' : 'Use this domain for words related to setting someone free.',
    'value' : '7.2.6.4 Set free'
  },
  {
    'guid' : '6330871b-6008-490e-bfff-e28e17ebce7e',
    'id' : '7.2.7',
    'code' : '7.2.7',
    'abbr' : '7.2.7',
    'name' : { 'en': 'Not moving' },
    'description' : 'Use this domain for words referring to not moving.',
    'value' : '7.2.7 Not moving'
  },
  {
    'guid' : '7110adbe-a4ce-47b0-8c2f-6b41edaf2fcb',
    'id' : '7.2.7.1',
    'code' : '7.2.7.1',
    'abbr' : '7.2.7.1',
    'name' : { 'en': 'Stop moving' },
    'description' : 'Use this domain for words referring to stopping moving.',
    'value' : '7.2.7.1 Stop moving'
  },
  {
    'guid' : '82eb3050-d382-48f6-a049-22a5f8a3b25a',
    'id' : '7.2.7.2',
    'code' : '7.2.7.2',
    'abbr' : '7.2.7.2',
    'name' : { 'en': 'Stay, remain' },
    'description' : 'Use this domain for words referring to not moving.',
    'value' : '7.2.7.2 Stay, remain'
  },
  {
    'guid' : 'd88d862c-01d4-4b43-9fbe-59208922e022',
    'id' : '7.2.7.3',
    'code' : '7.2.7.3',
    'abbr' : '7.2.7.3',
    'name' : { 'en': 'Wait' },
    'description' : 'Use this domain for words referring to waiting in a place, waiting to do something, or waiting for something to happen. The basic idea of waiting is for someone to not do anything for a period of time, because he expects something to happen that will cause him to do something.',
    'value' : '7.2.7.3 Wait'
  },
  {
    'guid' : '38ab2681-fcc7-4a75-bb0b-29c5cd2e3a8f',
    'id' : '7.2.8',
    'code' : '7.2.8',
    'abbr' : '7.2.8',
    'name' : { 'en': 'Send someone' },
    'description' : 'Use this domain for words related to causing someone to go somewhere.',
    'value' : '7.2.8 Send someone'
  },
  {
    'guid' : '2810998c-d6cc-47a3-a946-66d0986a2767',
    'id' : '7.3',
    'code' : '7.3',
    'abbr' : '7.3',
    'name' : { 'en': 'Move something' },
    'description' : 'Use this domain for general words referring to moving something or someone.',
    'value' : '7.3 Move something'
  },
  {
    'guid' : '6430b89c-7077-418a-a558-51f0e3f2c1a6',
    'id' : '7.3.1',
    'code' : '7.3.1',
    'abbr' : '7.3.1',
    'name' : { 'en': 'Carry' },
    'description' : 'Use this domain for words referring to carrying something--to pick something up and hold it while moving oneself.',
    'value' : '7.3.1 Carry'
  },
  {
    'guid' : '7831223b-e5ed-4186-a321-94e9ae72a27d',
    'id' : '7.3.1.1',
    'code' : '7.3.1.1',
    'abbr' : '7.3.1.1',
    'name' : { 'en': 'Throw' },
    'description' : 'Use this domain for words referring to throwing something.',
    'value' : '7.3.1.1 Throw'
  },
  {
    'guid' : 'ef025cd9-dd92-442b-a8f9-fe7ac944ccec',
    'id' : '7.3.1.2',
    'code' : '7.3.1.2',
    'abbr' : '7.3.1.2',
    'name' : { 'en': 'Catch' },
    'description' : 'Use this domain for verbs for catching something that is thrown or dropped',
    'value' : '7.3.1.2 Catch'
  },
  {
    'guid' : '05371057-2fe4-49ef-b203-f5bd6727645e',
    'id' : '7.3.1.3',
    'code' : '7.3.1.3',
    'abbr' : '7.3.1.3',
    'name' : { 'en': 'Shake' },
    'description' : 'Use this domain for words referring to moving something back and forth.',
    'value' : '7.3.1.3 Shake'
  },
  {
    'guid' : '06905a7e-47f4-4c86-afea-a4175295b566',
    'id' : '7.3.1.4',
    'code' : '7.3.1.4',
    'abbr' : '7.3.1.4',
    'name' : { 'en': 'Knock over' },
    'description' : 'Use this domain for words referring to knocking something over so that it falls, and for knocking a container over so that it spills its contents.',
    'value' : '7.3.1.4 Knock over'
  },
  {
    'guid' : '28a37d39-8347-4254-99bf-8e3c37dbf8a8',
    'id' : '7.3.1.5',
    'code' : '7.3.1.5',
    'abbr' : '7.3.1.5',
    'name' : { 'en': 'Set upright' },
    'description' : 'Use this domain for words related to setting something upright.',
    'value' : '7.3.1.5 Set upright'
  },
  {
    'guid' : 'd9f336cf-0682-4702-ab94-5ade755ddc64',
    'id' : '7.3.2',
    'code' : '7.3.2',
    'abbr' : '7.3.2',
    'name' : { 'en': 'Move something in a direction' },
    'description' : 'Use this domain for general words related to moving something in a direction.',
    'value' : '7.3.2 Move something in a direction'
  },
  {
    'guid' : 'ddef260e-b183-47f7-837e-165ffbd1af2c',
    'id' : '7.3.2.1',
    'code' : '7.3.2.1',
    'abbr' : '7.3.2.1',
    'name' : { 'en': 'Put in front' },
    'description' : 'Use this domain for words referring to putting something in front of you or in front of something else.',
    'value' : '7.3.2.1 Put in front'
  },
  {
    'guid' : 'b035dcf9-1dd0-4fb3-bd04-7c8945e92cdf',
    'id' : '7.3.2.2',
    'code' : '7.3.2.2',
    'abbr' : '7.3.2.2',
    'name' : { 'en': 'Put in back' },
    'description' : 'Use this domain for words referring to putting something in back of you or in back of something else.',
    'value' : '7.3.2.2 Put in back'
  },
  {
    'guid' : 'd975a233-1e48-4313-8bed-aada7460487e',
    'id' : '7.3.2.3',
    'code' : '7.3.2.3',
    'abbr' : '7.3.2.3',
    'name' : { 'en': 'Put aside' },
    'description' : 'Use this domain for words referring to putting something to the side of you.',
    'value' : '7.3.2.3 Put aside'
  },
  {
    'guid' : '7c7fc1b6-3775-4881-bbe0-9d0ce50e42a9',
    'id' : '7.3.2.4',
    'code' : '7.3.2.4',
    'abbr' : '7.3.2.4',
    'name' : { 'en': 'Lift' },
    'description' : 'Use this domain for words referring to putting something above you or above something else, putting something on top of something else, or moving something higher than it was.',
    'value' : '7.3.2.4 Lift'
  },
  {
    'guid' : 'f472b2d2-b4d3-4852-914d-71b66bdb6f26',
    'id' : '7.3.2.4.1',
    'code' : '7.3.2.4.1',
    'abbr' : '7.3.2.4.1',
    'name' : { 'en': 'Hang' },
    'description' : 'Use this domain for words referring to hanging something, and for words describing something that has been hung.',
    'value' : '7.3.2.4.1 Hang'
  },
  {
    'guid' : 'ab127348-7f98-43e5-801d-01241ecdb517',
    'id' : '7.3.2.5',
    'code' : '7.3.2.5',
    'abbr' : '7.3.2.5',
    'name' : { 'en': 'Lower something' },
    'description' : 'Use this domain for words referring to putting something lower than you or lower than something else, putting something under something else, or moving something lower than it was.',
    'value' : '7.3.2.5 Lower something'
  },
  {
    'guid' : '756f67e9-2b22-4c43-913c-ceff0e781545',
    'id' : '7.3.2.6',
    'code' : '7.3.2.6',
    'abbr' : '7.3.2.6',
    'name' : { 'en': 'Put in' },
    'description' : 'Use this domain for words referring to putting something inside something else.',
    'value' : '7.3.2.6 Put in'
  },
  {
    'guid' : '5a39edae-1ace-4889-b636-1dfd2a1bcc3c',
    'id' : '7.3.2.7',
    'code' : '7.3.2.7',
    'abbr' : '7.3.2.7',
    'name' : { 'en': 'Take something out of something' },
    'description' : 'Use this domain for words referring to taking something out of something else.',
    'value' : '7.3.2.7 Take something out of something'
  },
  {
    'guid' : '6bd023f6-730e-44c2-ae8f-78df967e2e18',
    'id' : '7.3.2.8',
    'code' : '7.3.2.8',
    'abbr' : '7.3.2.8',
    'name' : { 'en': 'Pull' },
    'description' : 'Use this domain for words related to pulling--causing something to move toward you.',
    'value' : '7.3.2.8 Pull'
  },
  {
    'guid' : '20baa5e7-4f02-4782-a292-c6281d7b5f3a',
    'id' : '7.3.2.9',
    'code' : '7.3.2.9',
    'abbr' : '7.3.2.9',
    'name' : { 'en': 'Push' },
    'description' : 'Use this domain for when someone or something causes something to move away from him.',
    'value' : '7.3.2.9 Push'
  },
  {
    'guid' : 'e931da8a-efc1-46cb-836a-72fba4a1eb4f',
    'id' : '7.3.3',
    'code' : '7.3.3',
    'abbr' : '7.3.3',
    'name' : { 'en': 'Take somewhere' },
    'description' : 'Use this domain for words referring to taking something or someone somewhere.',
    'value' : '7.3.3 Take somewhere'
  },
  {
    'guid' : '11cf45ec-f9d6-4c99-8782-738e26a342c8',
    'id' : '7.3.3.1',
    'code' : '7.3.3.1',
    'abbr' : '7.3.3.1',
    'name' : { 'en': 'Take something from somewhere' },
    'description' : 'Use this domain for words referring to taking something or someone from its place.',
    'value' : '7.3.3.1 Take something from somewhere'
  },
  {
    'guid' : '3180b6aa-3ad9-4bd3-96f7-ae72264406fb',
    'id' : '7.3.3.2',
    'code' : '7.3.3.2',
    'abbr' : '7.3.3.2',
    'name' : { 'en': 'Return something' },
    'description' : 'Use this domain for words referring to moving something back to an original place or person. (Something is in a place, someone (or something) moves it, then someone moves it back to the first place.)',
    'value' : '7.3.3.2 Return something'
  },
  {
    'guid' : '87c55aea-2c3f-44ce-81ec-18a153c5deb2',
    'id' : '7.3.3.3',
    'code' : '7.3.3.3',
    'abbr' : '7.3.3.3',
    'name' : { 'en': 'Send' },
    'description' : 'Use this domain for words related to sending something--to cause someone to take something somewhere.',
    'value' : '7.3.3.3 Send'
  },
  {
    'guid' : 'b79f8775-d8d0-4aa5-b4ab-917f6f3d6c13',
    'id' : '7.3.3.4',
    'code' : '7.3.3.4',
    'abbr' : '7.3.3.4',
    'name' : { 'en': 'Chase away' },
    'description' : 'Use this domain for words related to chasing or driving people and animals--to cause someone (or an animal) to move.',
    'value' : '7.3.3.4 Chase away'
  },
  {
    'guid' : 'bca1af45-7621-43c0-9152-fac0018e5319',
    'id' : '7.3.3.5',
    'code' : '7.3.3.5',
    'abbr' : '7.3.3.5',
    'name' : { 'en': 'Drive along' },
    'description' : 'Use this domain for words related to driving people and animals--to cause someone (or an animal) to move with you.',
    'value' : '7.3.3.5 Drive along'
  },
  {
    'guid' : '3b4b947a-f223-4c87-8839-9f6237cda9f6',
    'id' : '7.3.4',
    'code' : '7.3.4',
    'abbr' : '7.3.4',
    'name' : { 'en': 'Handle something' },
    'description' : 'Use this domain for general words for using the hands to move something.',
    'value' : '7.3.4 Handle something'
  },
  {
    'guid' : 'c35cba91-742c-4b98-b848-dfd520d959cf',
    'id' : '7.3.4.1',
    'code' : '7.3.4.1',
    'abbr' : '7.3.4.1',
    'name' : { 'en': 'Touch' },
    'description' : 'Use this domain for words referring to touching something with your hand without moving the thing.',
    'value' : '7.3.4.1 Touch'
  },
  {
    'guid' : '3885231e-8b18-4da3-af76-c75e8b731ed8',
    'id' : '7.3.4.2',
    'code' : '7.3.4.2',
    'abbr' : '7.3.4.2',
    'name' : { 'en': 'Pick up' },
    'description' : 'Use this domain for words related to picking something up.',
    'value' : '7.3.4.2 Pick up'
  },
  {
    'guid' : 'b7ed2482-6883-4a02-a992-e86c2573cc74',
    'id' : '7.3.4.3',
    'code' : '7.3.4.3',
    'abbr' : '7.3.4.3',
    'name' : { 'en': 'Put down' },
    'description' : 'Use this domain for words related to putting something down.',
    'value' : '7.3.4.3 Put down'
  },
  {
    'guid' : '1e419f7a-7363-46bc-8044-157ed0b40ccd',
    'id' : '7.3.4.4',
    'code' : '7.3.4.4',
    'abbr' : '7.3.4.4',
    'name' : { 'en': 'Hold' },
    'description' : 'Use this domain for holding something in the hand.',
    'value' : '7.3.4.4 Hold'
  },
  {
    'guid' : '5db1b502-7c36-44fb-a7b4-50744e9ec286',
    'id' : '7.3.4.5',
    'code' : '7.3.4.5',
    'abbr' : '7.3.4.5',
    'name' : { 'en': 'Actions of the hand' },
    'description' : 'Use this domain for the functions and actions of the hand.',
    'value' : '7.3.4.5 Actions of the hand'
  },
  {
    'guid' : 'eb821083-3fb0-441a-9f1d-ad2a9ed918d8',
    'id' : '7.3.4.6',
    'code' : '7.3.4.6',
    'abbr' : '7.3.4.6',
    'name' : { 'en': 'Support' },
    'description' : 'Use this domain for words related to keeping something from falling.',
    'value' : '7.3.4.6 Support'
  },
  {
    'guid' : 'df47f55d-b15d-4261-881e-3c4b0dc6d9be',
    'id' : '7.3.4.7',
    'code' : '7.3.4.7',
    'abbr' : '7.3.4.7',
    'name' : { 'en': 'Extend' },
    'description' : 'Use this domain for words related to extending something--to move something so that it covers a greater distance or area.',
    'value' : '7.3.4.7 Extend'
  },
  {
    'guid' : '40590157-9412-4558-b0f7-311867b649cc',
    'id' : '7.3.5',
    'code' : '7.3.5',
    'abbr' : '7.3.5',
    'name' : { 'en': 'Turn something' },
    'description' : 'Use this domain for turning something--to cause something to change direction, or to cause something to revolve or move in a circle.',
    'value' : '7.3.5 Turn something'
  },
  {
    'guid' : '8b9f23f4-a147-4ea8-a13a-c5b1edc7f5e4',
    'id' : '7.3.6',
    'code' : '7.3.6',
    'abbr' : '7.3.6',
    'name' : { 'en': 'Open' },
    'description' : 'Use this domain for words related to opening something.',
    'value' : '7.3.6 Open'
  },
  {
    'guid' : 'd7e4bf3e-e539-43bc-bb43-3ae0980ffb86',
    'id' : '7.3.6.1',
    'code' : '7.3.6.1',
    'abbr' : '7.3.6.1',
    'name' : { 'en': 'Shut, close' },
    'description' : 'Use this domain for words related to shutting something.',
    'value' : '7.3.6.1 Shut, close'
  },
  {
    'guid' : '7992958d-fd36-469e-a94b-f8a9eb26af64',
    'id' : '7.3.6.2',
    'code' : '7.3.6.2',
    'abbr' : '7.3.6.2',
    'name' : { 'en': 'Block, dam up' },
    'description' : 'Use this domain for words referring to preventing someone or something from moving',
    'value' : '7.3.6.2 Block, dam up'
  },
  {
    'guid' : '440608df-3c98-4dc8-9fd3-fad08afe7aef',
    'id' : '7.3.6.3',
    'code' : '7.3.6.3',
    'abbr' : '7.3.6.3',
    'name' : { 'en': 'Limit' },
    'description' : 'Use this domain for words referring to a limit beyond which something may or should not go, and for words referring to imposing a limit.',
    'value' : '7.3.6.3 Limit'
  },
  {
    'guid' : '7c80f6ee-e76d-4903-aee6-7a33d1da3f75',
    'id' : '7.3.7',
    'code' : '7.3.7',
    'abbr' : '7.3.7',
    'name' : { 'en': 'Cover' },
    'description' : 'Use this domain for words related to covering something.',
    'value' : '7.3.7 Cover'
  },
  {
    'guid' : '5b925ee1-82ef-4692-bba2-9ce3cb41c7bb',
    'id' : '7.3.7.1',
    'code' : '7.3.7.1',
    'abbr' : '7.3.7.1',
    'name' : { 'en': 'Uncover' },
    'description' : 'Use this domain for words related to uncovering something.',
    'value' : '7.3.7.1 Uncover'
  },
  {
    'guid' : '2608bcf8-ed20-4501-8510-4ecacf922dd4',
    'id' : '7.3.7.2',
    'code' : '7.3.7.2',
    'abbr' : '7.3.7.2',
    'name' : { 'en': 'Wrap' },
    'description' : 'Use this domain for words related to wrapping something--to cover something on all sides with something like leaves, cloth, or paper.',
    'value' : '7.3.7.2 Wrap'
  },
  {
    'guid' : 'a2240259-608b-40f1-990a-7f8e00ef1d07',
    'id' : '7.3.7.3',
    'code' : '7.3.7.3',
    'abbr' : '7.3.7.3',
    'name' : { 'en': 'Spread, smear' },
    'description' : 'Use this domain for words related to spreading something--to cover something on all sides with something liquid or sticky like paint or mud.',
    'value' : '7.3.7.3 Spread, smear'
  },
  {
    'guid' : '991357dc-9f56-47ed-8790-85cbd5f9b06f',
    'id' : '7.3.8',
    'code' : '7.3.8',
    'abbr' : '7.3.8',
    'name' : { 'en': 'Transport' },
    'description' : 'Use this domain for words referring to moving something in a vehicle.',
    'value' : '7.3.8 Transport'
  },
  {
    'guid' : '24398eec-edd1-449a-ad36-d609be24a79e',
    'id' : '7.4',
    'code' : '7.4',
    'abbr' : '7.4',
    'name' : { 'en': 'Have, be with' },
    'description' : 'Use this domain for words related to having something.',
    'value' : '7.4 Have, be with'
  },
  {
    'guid' : '56d1a950-8798-45fb-bccd-d8b1eb37c071',
    'id' : '7.4.1',
    'code' : '7.4.1',
    'abbr' : '7.4.1',
    'name' : { 'en': 'Give, hand to' },
    'description' : 'Use this domain for words referring to giving something to someone, in which there is no transaction of ownership, merely the movement of the thing from one person to another.',
    'value' : '7.4.1 Give, hand to'
  },
  {
    'guid' : 'c96ac1eb-12f2-47af-9e96-9d99fce7e8f5',
    'id' : '7.4.2',
    'code' : '7.4.2',
    'abbr' : '7.4.2',
    'name' : { 'en': 'Receive' },
    'description' : 'Use this domain for words related to receiving something from someone.',
    'value' : '7.4.2 Receive'
  },
  {
    'guid' : 'adfc2bcd-6b8e-486c-b105-29b286b61cc0',
    'id' : '7.4.3',
    'code' : '7.4.3',
    'abbr' : '7.4.3',
    'name' : { 'en': 'Get' },
    'description' : 'Use this domain for words referring to getting something.',
    'value' : '7.4.3 Get'
  },
  {
    'guid' : '86c065ea-2420-4619-82d4-1d43527b3371',
    'id' : '7.4.4',
    'code' : '7.4.4',
    'abbr' : '7.4.4',
    'name' : { 'en': 'Distribute' },
    'description' : 'Use this domain for words related to distributing things to several people.',
    'value' : '7.4.4 Distribute'
  },
  {
    'guid' : '7df3078c-f681-4123-a712-4b83e438ea1d',
    'id' : '7.4.5',
    'code' : '7.4.5',
    'abbr' : '7.4.5',
    'name' : { 'en': 'Keep something' },
    'description' : 'Use this domain for words related to keeping something.',
    'value' : '7.4.5 Keep something'
  },
  {
    'guid' : '1b3dccfe-29e4-478e-8443-17be9454a05a',
    'id' : '7.4.5.1',
    'code' : '7.4.5.1',
    'abbr' : '7.4.5.1',
    'name' : { 'en': 'Leave something' },
    'description' : 'Use this domain for words referring to leaving something or someone in a place and going away.',
    'value' : '7.4.5.1 Leave something'
  },
  {
    'guid' : '6c32038c-adf3-4085-bde3-cd2f21a421ba',
    'id' : '7.4.5.2',
    'code' : '7.4.5.2',
    'abbr' : '7.4.5.2',
    'name' : { 'en': 'Throw away' },
    'description' : 'Use this domain for throwing away something that you no longer want.',
    'value' : '7.4.5.2 Throw away'
  },
  {
    'guid' : 'e7119442-3063-422a-a03e-d02e570ccd0f',
    'id' : '7.4.6',
    'code' : '7.4.6',
    'abbr' : '7.4.6',
    'name' : { 'en': 'Not have' },
    'description' : 'Use this domain for words related to having something.',
    'value' : '7.4.6 Not have'
  },
  {
    'guid' : '6045c6eb-efea-4586-95f8-840d32578d66',
    'id' : '7.5',
    'code' : '7.5',
    'abbr' : '7.5',
    'name' : { 'en': 'Arrange' },
    'description' : 'Use this domain for words referring to arranging things--to physically move a group of things or people and put them in a pattern.',
    'value' : '7.5 Arrange'
  },
  {
    'guid' : 'c16334a0-be29-4a1e-a870-4cb3f1df984d',
    'id' : '7.5.1',
    'code' : '7.5.1',
    'abbr' : '7.5.1',
    'name' : { 'en': 'Gather' },
    'description' : 'Use this domain for words referring to gathering things into a group. The basic idea of this domain involves a situation in which two or more things are not together, and someone moves them so that they are together.',
    'value' : '7.5.1 Gather'
  },
  {
    'guid' : '30b3faa8-747e-465f-833a-a9957a259be2',
    'id' : '7.5.1.1',
    'code' : '7.5.1.1',
    'abbr' : '7.5.1.1',
    'name' : { 'en': 'Separate, scatter' },
    'description' : 'Use this domain for words referring to separating things into groups, and scattering things. The basic idea of this domain involves a situation in which two or more things are together, and someone moves them so that they are no longer together.',
    'value' : '7.5.1.1 Separate, scatter'
  },
  {
    'guid' : '71b19b9e-231c-4196-a7d7-aa56a5079782',
    'id' : '7.5.1.2',
    'code' : '7.5.1.2',
    'abbr' : '7.5.1.2',
    'name' : { 'en': 'Include' },
    'description' : 'Use this domain for words related to including something in a group.',
    'value' : '7.5.1.2 Include'
  },
  {
    'guid' : 'b7f058af-9ce6-4dd0-b555-00526975300e',
    'id' : '7.5.1.3',
    'code' : '7.5.1.3',
    'abbr' : '7.5.1.3',
    'name' : { 'en': 'Special' },
    'description' : 'Use this domain for words that describe a member of a group that is different or special.',
    'value' : '7.5.1.3 Special'
  },
  {
    'guid' : '08239f53-daa5-47a6-9f39-29a9064b0c27',
    'id' : '7.5.2',
    'code' : '7.5.2',
    'abbr' : '7.5.2',
    'name' : { 'en': 'Join, attach' },
    'description' : 'Use this domain for words related to joining two or more things together.',
    'value' : '7.5.2 Join, attach'
  },
  {
    'guid' : '6ab060ca-ecfc-4a46-accb-42b0473998cd',
    'id' : '7.5.2.1',
    'code' : '7.5.2.1',
    'abbr' : '7.5.2.1',
    'name' : { 'en': 'Link, connect' },
    'description' : 'Use this domain for words related to a linking or connecting things together--to put something like a road, pipe, or wire between things so that people or things can move between them.',
    'value' : '7.5.2.1 Link, connect'
  },
  {
    'guid' : 'e43c9905-ae67-4627-8b10-bd7a453828b4',
    'id' : '7.5.2.2',
    'code' : '7.5.2.2',
    'abbr' : '7.5.2.2',
    'name' : { 'en': 'Stick together' },
    'description' : 'Use this domain for words referring to two or more things cohering or sticking together.',
    'value' : '7.5.2.2 Stick together'
  },
  {
    'guid' : '2b27b8ca-188e-44ad-aa86-ffa1f99106e3',
    'id' : '7.5.2.3',
    'code' : '7.5.2.3',
    'abbr' : '7.5.2.3',
    'name' : { 'en': 'Add to something' },
    'description' : 'Use this domain for words related to adding something to another thing.',
    'value' : '7.5.2.3 Add to something'
  },
  {
    'guid' : 'a31c85df-02a9-4dd8-a094-0f07a0afbcca',
    'id' : '7.5.2.4',
    'code' : '7.5.2.4',
    'abbr' : '7.5.2.4',
    'name' : { 'en': 'Remove, take apart' },
    'description' : 'Use this domain for words related to removing part of something, taking something apart, and things coming apart.',
    'value' : '7.5.2.4 Remove, take apart'
  },
  {
    'guid' : 'fe58ae61-ab0b-43a4-86fb-d9aedd199932',
    'id' : '7.5.3',
    'code' : '7.5.3',
    'abbr' : '7.5.3',
    'name' : { 'en': 'Mix' },
    'description' : 'Use this domain for words related to mixing things together--to put two or more different kinds of substances, like liquids or cooking ingredients,  together.',
    'value' : '7.5.3 Mix'
  },
  {
    'guid' : '9b158c9e-9ba5-4be2-a9a1-77ef888a3b06',
    'id' : '7.5.3.1',
    'code' : '7.5.3.1',
    'abbr' : '7.5.3.1',
    'name' : { 'en': 'Pure, unmixed' },
    'description' : 'Use this domain for words related to being pure--words describing something (like water, food, or air) that does not have anything bad in it; or unmixed--words describing something that is only one thing and has not been mixed with something else.',
    'value' : '7.5.3.1 Pure, unmixed'
  },
  {
    'guid' : '9b267cc1-983c-407a-98d2-1e27add6292c',
    'id' : '7.5.4',
    'code' : '7.5.4',
    'abbr' : '7.5.4',
    'name' : { 'en': 'Tie' },
    'description' : 'Use this domain for words related to tying things together.',
    'value' : '7.5.4 Tie'
  },
  {
    'guid' : '761706fe-d289-4ace-b2c3-ab15d80dba7f',
    'id' : '7.5.4.1',
    'code' : '7.5.4.1',
    'abbr' : '7.5.4.1',
    'name' : { 'en': 'Rope, string' },
    'description' : 'Use this domain for words referring to rope, string, and other things used to tie things together.',
    'value' : '7.5.4.1 Rope, string'
  },
  {
    'guid' : '5f791daf-98a2-4787-93cc-8813aea93c4d',
    'id' : '7.5.4.2',
    'code' : '7.5.4.2',
    'abbr' : '7.5.4.2',
    'name' : { 'en': 'Tangle' },
    'description' : 'Use this domain for words related to becoming tangled--when something long and thin, such as rope, string, thread, hair, grass, or vines, becomes disorganized, twisted, or knotted, so that it is hard to separate it.',
    'value' : '7.5.4.2 Tangle'
  },
  {
    'guid' : 'e6c9fe4c-199e-4934-b622-739a85b0830d',
    'id' : '7.5.5',
    'code' : '7.5.5',
    'abbr' : '7.5.5',
    'name' : { 'en': 'Organize' },
    'description' : 'Use this domain for words referring to organizing things, people, events, and ideas--to think about and decide on the pattern a group of things should be arranged in, and then doing whatever is necessary to arrange them so that they can be used for some purpose. This domain is like the domain "Arrange" except that "Arrange" emphasizes physically moving the things, and the domain "Organize" emphasizes the logical system. "Organize" does not require that the things be moved, only that the logical pattern is specified.',
    'value' : '7.5.5 Organize'
  },
  {
    'guid' : '583c98ff-1cc8-4b05-9086-974d13a78894',
    'id' : '7.5.5.1',
    'code' : '7.5.5.1',
    'abbr' : '7.5.5.1',
    'name' : { 'en': 'Disorganized' },
    'description' : 'Use this domain for words related to things becoming disorganized.',
    'value' : '7.5.5.1 Disorganized'
  },
  {
    'guid' : 'b3fb9960-8f42-43bc-9595-dfb3e04f5bfd',
    'id' : '7.5.6',
    'code' : '7.5.6',
    'abbr' : '7.5.6',
    'name' : { 'en': 'Substitute' },
    'description' : 'Use this domain for words referring to substituting something for something else--to move something and put something else in its place.',
    'value' : '7.5.6 Substitute'
  },
  {
    'guid' : 'be2f2785-7219-4a35-b8d3-aa56b9b78514',
    'id' : '7.5.7',
    'code' : '7.5.7',
    'abbr' : '7.5.7',
    'name' : { 'en': 'Multiple things moving' },
    'description' : 'Use this domain for words referring to multiple things moving.',
    'value' : '7.5.7 Multiple things moving'
  },
  {
    'guid' : '467dd680-ac64-4dc4-8a17-1cfe297d3392',
    'id' : '7.5.8',
    'code' : '7.5.8',
    'abbr' : '7.5.8',
    'name' : { 'en': 'Simple, complicated' },
    'description' : 'Use this domain for words related to being simple or complicated--words describing the organization of a group of things.',
    'value' : '7.5.8 Simple, complicated'
  },
  {
    'guid' : '3fae9066-eb66-444e-bd41-818b9f7b3bae',
    'id' : '7.5.9',
    'code' : '7.5.9',
    'abbr' : '7.5.9',
    'name' : { 'en': 'Put' },
    'description' : 'Use this domain for words related to putting something somewhere.',
    'value' : '7.5.9 Put'
  },
  {
    'guid' : '4068488f-59e9-47d1-8884-a1d6dcc10c36',
    'id' : '7.5.9.1',
    'code' : '7.5.9.1',
    'abbr' : '7.5.9.1',
    'name' : { 'en': 'Load, pile' },
    'description' : 'Use this domain for words related to putting lots of things on something.',
    'value' : '7.5.9.1 Load, pile'
  },
  {
    'guid' : 'dfe59469-d1bf-4ed2-9faa-6d5af52eefdd',
    'id' : '7.5.9.2',
    'code' : '7.5.9.2',
    'abbr' : '7.5.9.2',
    'name' : { 'en': 'Fill, cover' },
    'description' : 'Use this domain for words related to filling a container or covering an area with something.',
    'value' : '7.5.9.2 Fill, cover'
  },
  {
    'guid' : '043d12ac-c76d-4b4c-813b-4ef7758c8085',
    'id' : '7.6',
    'code' : '7.6',
    'abbr' : '7.6',
    'name' : { 'en': 'Hide' },
    'description' : 'Use this domain for words related to hiding things so that they cannot be seen or found, and for hiding oneself.',
    'value' : '7.6 Hide'
  },
  {
    'guid' : '0ce61f27-9de8-49b2-9189-6f6efe488f6d',
    'id' : '7.6.1',
    'code' : '7.6.1',
    'abbr' : '7.6.1',
    'name' : { 'en': 'Search' },
    'description' : 'Use this domain for words related to searching for something that has been hidden or lost.',
    'value' : '7.6.1 Search'
  },
  {
    'guid' : 'bd9de99f-6a92-47ee-b6bc-e9877ea21202',
    'id' : '7.6.2',
    'code' : '7.6.2',
    'abbr' : '7.6.2',
    'name' : { 'en': 'Find' },
    'description' : 'Use this domain for words related to finding something that has been hidden or lost.',
    'value' : '7.6.2 Find'
  },
  {
    'guid' : 'b7f4fd44-fa17-46a8-bdaf-d3399d6cb0ac',
    'id' : '7.6.3',
    'code' : '7.6.3',
    'abbr' : '7.6.3',
    'name' : { 'en': 'Lose, misplace' },
    'description' : 'Use this domain for words referring to putting something in a place and not being able to find it again, or for when someone else moves something without your knowledge so that you cannot find it.',
    'value' : '7.6.3 Lose, misplace'
  },
  {
    'guid' : '66d8b546-92f5-4e94-b992-08be81c3d30c',
    'id' : '7.7',
    'code' : '7.7',
    'abbr' : '7.7',
    'name' : { 'en': 'Physical impact' },
    'description' : 'Use this domain for general words referring to making a physical impact on  something, including the words for the action itself and the result of the  action.',
    'value' : '7.7 Physical impact'
  },
  {
    'guid' : 'd25f7907-091e-4cf7-bd8c-bdb97278b616',
    'id' : '7.7.1',
    'code' : '7.7.1',
    'abbr' : '7.7.1',
    'name' : { 'en': 'Hit' },
    'description' : 'Use this domain for words related to hitting something.',
    'value' : '7.7.1 Hit'
  },
  {
    'guid' : '5718fcc8-1eba-4b8d-9b6b-0c8349f53f80',
    'id' : '7.7.2',
    'code' : '7.7.2',
    'abbr' : '7.7.2',
    'name' : { 'en': 'Aim at a target' },
    'description' : 'Use this domain for words referring to aiming at a target, and for hitting or missing the target.',
    'value' : '7.7.2 Aim at a target'
  },
  {
    'guid' : 'be4a63e7-f4ba-4de2-be69-d26219d99cb6',
    'id' : '7.7.3',
    'code' : '7.7.3',
    'abbr' : '7.7.3',
    'name' : { 'en': 'Kick' },
    'description' : 'Use this domain for words referring to kicking--to hit something with your foot. The words in this domain may be distinguished by the way in which the foot moves, either in a swinging motion, or by first bending the leg and then quickly straightening it. They may also be distinguished by whether the foot moves horizontally or vertically, whether the effect is to move something or damage it, or how hard the person kicks.',
    'value' : '7.7.3 Kick'
  },
  {
    'guid' : '0a85ee64-e466-4295-8e2c-5b06c8e3054f',
    'id' : '7.7.4',
    'code' : '7.7.4',
    'abbr' : '7.7.4',
    'name' : { 'en': 'Press' },
    'description' : 'Use this domain for words referring to pressing something.',
    'value' : '7.7.4 Press'
  },
  {
    'guid' : '709d43dd-ce94-4df1-91b1-edb0b12fdaea',
    'id' : '7.7.5',
    'code' : '7.7.5',
    'abbr' : '7.7.5',
    'name' : { 'en': 'Rub' },
    'description' : 'Use this domain for words referring to rubbing--to move something smooth against something else, in order to make it smooth or clean.',
    'value' : '7.7.5 Rub'
  },
  {
    'guid' : 'a3ba23d2-618e-4618-af18-9befae2f888b',
    'id' : '7.7.6',
    'code' : '7.7.6',
    'abbr' : '7.7.6',
    'name' : { 'en': 'Grind' },
    'description' : 'Use this domain for words referring to grinding--to rub something rough against something else, while applying force, in order to break it or remove its surface.',
    'value' : '7.7.6 Grind'
  },
  {
    'guid' : '06be473e-c2f3-45fe-8522-3a0c033b5067',
    'id' : '7.7.7',
    'code' : '7.7.7',
    'abbr' : '7.7.7',
    'name' : { 'en': 'Mark' },
    'description' : 'Use this domain for words related to making a mark on something.',
    'value' : '7.7.7 Mark'
  },
  {
    'guid' : 'e5f9c9cf-0b0c-47aa-b7df-8c37f211cd00',
    'id' : '7.8',
    'code' : '7.8',
    'abbr' : '7.8',
    'name' : { 'en': 'Divide into pieces' },
    'description' : 'Use this domain for words referring to dividing something into parts or pieces, perhaps with the added idea of using care, or into a certain number of parts.',
    'value' : '7.8 Divide into pieces'
  },
  {
    'guid' : '23b1a6b4-8d91-425c-b8c2-52d06b1c1d23',
    'id' : '7.8.1',
    'code' : '7.8.1',
    'abbr' : '7.8.1',
    'name' : { 'en': 'Break' },
    'description' : 'Use this domain for words referring breaking something into pieces, perhaps with the added idea of doing it accidentally or without being careful.',
    'value' : '7.8.1 Break'
  },
  {
    'guid' : 'b62b5fc7-1b20-4f63-8459-8eb4991839ee',
    'id' : '7.8.2',
    'code' : '7.8.2',
    'abbr' : '7.8.2',
    'name' : { 'en': 'Crack' },
    'description' : 'Use this domain for words referring to a crack--a partial break in something that does not entirely divide it in pieces.',
    'value' : '7.8.2 Crack'
  },
  {
    'guid' : '42be1634-72ca-4a20-80a1-ba726e5cd1d2',
    'id' : '7.8.3',
    'code' : '7.8.3',
    'abbr' : '7.8.3',
    'name' : { 'en': 'Cut' },
    'description' : 'Use this domain for words related to cutting something.',
    'value' : '7.8.3 Cut'
  },
  {
    'guid' : 'b0b161b2-e773-4b04-99eb-23778fd2aa80',
    'id' : '7.8.4',
    'code' : '7.8.4',
    'abbr' : '7.8.4',
    'name' : { 'en': 'Tear, rip' },
    'description' : 'Use this domain for words related to tearing or ripping something.',
    'value' : '7.8.4 Tear, rip'
  },
  {
    'guid' : '313a65bf-450f-48da-8903-a43247f1a5f8',
    'id' : '7.8.5',
    'code' : '7.8.5',
    'abbr' : '7.8.5',
    'name' : { 'en': 'Make hole, opening' },
    'description' : 'Use this domain for words related to making a hole or opening in something.',
    'value' : '7.8.5 Make hole, opening'
  },
  {
    'guid' : 'c5b8c936-1e01-4e86-9145-a2b721ec9e39',
    'id' : '7.8.6',
    'code' : '7.8.6',
    'abbr' : '7.8.6',
    'name' : { 'en': 'Dig' },
    'description' : 'Use this domain for words related to digging in the ground.',
    'value' : '7.8.6 Dig'
  },
  {
    'guid' : 'f5156cde-9735-4249-920d-597fb0a7a8e3',
    'id' : '7.9',
    'code' : '7.9',
    'abbr' : '7.9',
    'name' : { 'en': 'Break, wear out' },
    'description' : 'Use this domain for words related to something breaking or wearing out, especially for things that people make and use.',
    'value' : '7.9 Break, wear out'
  },
  {
    'guid' : 'eb00b4c2-5b87-4ef8-9548-800fc5c9b524',
    'id' : '7.9.1',
    'code' : '7.9.1',
    'abbr' : '7.9.1',
    'name' : { 'en': 'Damage' },
    'description' : 'Use this domain for words referring to damaging something--to do something bad to something, but not completely ruin it so that it can"t be used any more.',
    'value' : '7.9.1 Damage'
  },
  {
    'guid' : '1a28d255-8f58-428c-9641-59f17f8b1e08',
    'id' : '7.9.2',
    'code' : '7.9.2',
    'abbr' : '7.9.2',
    'name' : { 'en': 'Tear down' },
    'description' : 'Use this domain for words referring to tearing down buildings and other structures.',
    'value' : '7.9.2 Tear down'
  },
  {
    'guid' : 'ca98bd7b-8711-41f6-86d0-5cd07b7bfe0d',
    'id' : '7.9.3',
    'code' : '7.9.3',
    'abbr' : '7.9.3',
    'name' : { 'en': 'Destroy' },
    'description' : 'Use this domain for words referring to destroying something--to damage something so that it is beyond repair and cannot be used.',
    'value' : '7.9.3 Destroy'
  },
  {
    'guid' : '41cac849-613d-4be4-a3bc-389412b7f653',
    'id' : '7.9.4',
    'code' : '7.9.4',
    'abbr' : '7.9.4',
    'name' : { 'en': 'Repair' },
    'description' : 'Use this domain for words related to repairing something.',
    'value' : '7.9.4 Repair'
  },
  {
    'guid' : 'c72985cf-b07f-4ed5-873a-a2209929667e',
    'id' : '8',
    'code' : '8',
    'abbr' : '8',
    'name' : { 'en': 'States' },
    'description' : 'Use this domain for general words refer to the state or condition of something.',
    'value' : '8 States'
  },
  {
    'guid' : 'c6c772af-7b6b-4393-b0da-5b4a329d3426',
    'id' : '8.1',
    'code' : '8.1',
    'abbr' : '8.1',
    'name' : { 'en': 'Quantity' },
    'description' : 'Use this domain for general words referring to the amount or quantity of something.',
    'value' : '8.1 Quantity'
  },
  {
    'guid' : 'fb84538a-17a8-4adc-8d50-e2b66f8e4099',
    'id' : '8.1.1',
    'code' : '8.1.1',
    'abbr' : '8.1.1',
    'name' : { 'en': 'Number' },
    'description' : 'Use this domain for words related to numbers. Every language has a word for "one", and a word for "two". These two numbers often have special words. So we have included a separate domain for "one" and "two". In addition the numbers form a series (one, two, three...). Most languages have more than one series of numbers (first, second, third...).  So we have also included a domain for each series of numbers that most languages have. Your language may have other series of numbers. Put them in the domain "Number series".',
    'value' : '8.1.1 Number'
  },
  {
    'guid' : 'b6686c7c-39de-40b5-adee-67fc7dc54374',
    'id' : '8.1.1.1',
    'code' : '8.1.1.1',
    'abbr' : '8.1.1.1',
    'name' : { 'en': 'Cardinal numbers' },
    'description' : 'Use this domain for words related to the cardinal numbers (one, two, three)--the numbers used to count.',
    'value' : '8.1.1.1 Cardinal numbers'
  },
  {
    'guid' : '2c04fa05-eebf-4331-b392-23f795c32382',
    'id' : '8.1.1.1.1',
    'code' : '8.1.1.1.1',
    'abbr' : '8.1.1.1.1',
    'name' : { 'en': 'One' },
    'description' : 'Use this domain for words related to the number one. It appears that every language has a word for "one" and "two", but not every language has a word for the numbers higher than two. Many languages have several words that mean "one".',
    'value' : '8.1.1.1.1 One'
  },
  {
    'guid' : 'd086c2ad-2d11-4250-a25a-dc6538439db6',
    'id' : '8.1.1.1.2',
    'code' : '8.1.1.1.2',
    'abbr' : '8.1.1.1.2',
    'name' : { 'en': 'Two' },
    'description' : 'Use this domain for words related to the number two.',
    'value' : '8.1.1.1.2 Two'
  },
  {
    'guid' : '816490a4-2cac-472a-bbb0-eafbb9bbe4a8',
    'id' : '8.1.1.2',
    'code' : '8.1.1.2',
    'abbr' : '8.1.1.2',
    'name' : { 'en': 'Ordinal numbers' },
    'description' : 'Use this domain for words related to the ordinal numbers (first, second, third)--the numbers used to indicate the order of something in a series, such as "the first child".',
    'value' : '8.1.1.2 Ordinal numbers'
  },
  {
    'guid' : 'd85d0838-a9e4-4787-8fce-7d0466bc24b9',
    'id' : '8.1.1.3',
    'code' : '8.1.1.3',
    'abbr' : '8.1.1.3',
    'name' : { 'en': 'Number of times' },
    'description' : 'Use this domain for numbers that refer to the number of times something happens or is done. These numbers may be adverbs as in English (twice) or in some languages they may be verbs (to do something two times).',
    'value' : '8.1.1.3 Number of times'
  },
  {
    'guid' : 'a57185c3-0cb5-41fa-94bf-da0c9edac600',
    'id' : '8.1.1.4',
    'code' : '8.1.1.4',
    'abbr' : '8.1.1.4',
    'name' : { 'en': 'Multiples' },
    'description' : 'Use this domain for words related to multiples of something (single, double, triple).',
    'value' : '8.1.1.4 Multiples'
  },
  {
    'guid' : 'ffa13b7d-5eaa-43be-8518-51d9aa08f321',
    'id' : '8.1.1.5',
    'code' : '8.1.1.5',
    'abbr' : '8.1.1.5',
    'name' : { 'en': 'Numbered group' },
    'description' : 'Use this domain for words related to a group of a particular number of things or people (solo, duo, trio; alone, in threes).',
    'value' : '8.1.1.5 Numbered group'
  },
  {
    'guid' : '64e6c0db-6dd6-4b80-bbe9-c96bb161674b',
    'id' : '8.1.1.6',
    'code' : '8.1.1.6',
    'abbr' : '8.1.1.6',
    'name' : { 'en': 'Fraction' },
    'description' : 'Use this domain for the numbers that refer to a fraction of something.',
    'value' : '8.1.1.6 Fraction'
  },
  {
    'guid' : '0e250e72-6c3f-424f-9e62-2dcc9729d817',
    'id' : '8.1.1.7',
    'code' : '8.1.1.7',
    'abbr' : '8.1.1.7',
    'name' : { 'en': 'Number series' },
    'description' : 'Use this domain for other series of numbers that are different from those in the previous domains.',
    'value' : '8.1.1.7 Number series'
  },
  {
    'guid' : 'c0d903bf-6502-45dd-9dd8-cff7f022c696',
    'id' : '8.1.2',
    'code' : '8.1.2',
    'abbr' : '8.1.2',
    'name' : { 'en': 'Count' },
    'description' : 'Use this domain for words related to counting--to say the numbers in order, or to use numbers to find an amount.',
    'value' : '8.1.2 Count'
  },
  {
    'guid' : '8bdf4847-903b-4af6-9553-3cfab65de516',
    'id' : '8.1.2.1',
    'code' : '8.1.2.1',
    'abbr' : '8.1.2.1',
    'name' : { 'en': 'Mathematics' },
    'description' : 'Use this domain for words related to mathematics and arithmetic--the study of numbers.',
    'value' : '8.1.2.1 Mathematics'
  },
  {
    'guid' : '83899b19-8b39-4bf0-b124-4c6188569ec8',
    'id' : '8.1.2.2',
    'code' : '8.1.2.2',
    'abbr' : '8.1.2.2',
    'name' : { 'en': 'Add numbers' },
    'description' : 'Use this domain for words related to adding two numbers together.',
    'value' : '8.1.2.2 Add numbers'
  },
  {
    'guid' : '7cf6312e-f9f0-49e8-ae60-7677fac86c3f',
    'id' : '8.1.2.3',
    'code' : '8.1.2.3',
    'abbr' : '8.1.2.3',
    'name' : { 'en': 'Subtract numbers' },
    'description' : 'Use this domain for words related to subtracting one number from another.',
    'value' : '8.1.2.3 Subtract numbers'
  },
  {
    'guid' : '57f07b5f-75bf-4565-b969-ce0adc0b50d4',
    'id' : '8.1.2.4',
    'code' : '8.1.2.4',
    'abbr' : '8.1.2.4',
    'name' : { 'en': 'Multiply numbers' },
    'description' : 'Use this domain for words related to multiplying one number times another.',
    'value' : '8.1.2.4 Multiply numbers'
  },
  {
    'guid' : '7b17e304-9a3f-463f-8216-cd1e1e119e0e',
    'id' : '8.1.2.5',
    'code' : '8.1.2.5',
    'abbr' : '8.1.2.5',
    'name' : { 'en': 'Divide numbers' },
    'description' : 'Use this domain for words related to dividing one number by another number.',
    'value' : '8.1.2.5 Divide numbers'
  },
  {
    'guid' : '0ebf9fcc-ee38-4f5f-ab5e-c76e199ef7ae',
    'id' : '8.1.3',
    'code' : '8.1.3',
    'abbr' : '8.1.3',
    'name' : { 'en': 'Plural' },
    'description' : 'Use this domain for words and affixes that indicate that there is more than one of something. Some languages, such as Indo-European languages,  indicate plural with an affix. Other languages, such as Austronesian languages, use a separate word. Some languages also have words or affixes that indicate that there are two of something.',
    'value' : '8.1.3 Plural'
  },
  {
    'guid' : '1b6b0c12-9ecd-45cb-bb0e-0dadb435eddf',
    'id' : '8.1.3.1',
    'code' : '8.1.3.1',
    'abbr' : '8.1.3.1',
    'name' : { 'en': 'Many, much' },
    'description' : 'Use this domain for words indicating that there are many things or people, or that there is much of something.',
    'value' : '8.1.3.1 Many, much'
  },
  {
    'guid' : '36934fab-c0ed-4f25-a387-e1cca26b2401',
    'id' : '8.1.3.2',
    'code' : '8.1.3.2',
    'abbr' : '8.1.3.2',
    'name' : { 'en': 'Few, little' },
    'description' : 'Use this domain for words related there being few or little of something.',
    'value' : '8.1.3.2 Few, little'
  },
  {
    'guid' : 'd8ea1902-5fa6-4fda-b7b2-1f9c301cdc5f',
    'id' : '8.1.3.3',
    'code' : '8.1.3.3',
    'abbr' : '8.1.3.3',
    'name' : { 'en': 'Group of things' },
    'description' : 'Use this domain for words that refer to a group of things.',
    'value' : '8.1.3.3 Group of things'
  },
  {
    'guid' : '8216627d-9d20-4a5c-8bfd-0709c16e7a08',
    'id' : '8.1.4',
    'code' : '8.1.4',
    'abbr' : '8.1.4',
    'name' : { 'en': 'More' },
    'description' : 'Use this domain for words related to there being more of something.',
    'value' : '8.1.4 More'
  },
  {
    'guid' : 'fcc204a3-eae4-46d1-a9dc-08864fde1772',
    'id' : '8.1.4.1',
    'code' : '8.1.4.1',
    'abbr' : '8.1.4.1',
    'name' : { 'en': 'Less' },
    'description' : 'Use this domain for words that indicate that the number or amount of something is less than the number or amount of another thing.',
    'value' : '8.1.4.1 Less'
  },
  {
    'guid' : 'f2022802-4f43-4fa2-8c58-33a8b9e75895',
    'id' : '8.1.4.2',
    'code' : '8.1.4.2',
    'abbr' : '8.1.4.2',
    'name' : { 'en': 'Increase' },
    'description' : 'Use this domain for words related to something increasing in number or amount--to be more than before.',
    'value' : '8.1.4.2 Increase'
  },
  {
    'guid' : '1ff743cb-49e0-483d-8a1d-4603a7d6c395',
    'id' : '8.1.4.3',
    'code' : '8.1.4.3',
    'abbr' : '8.1.4.3',
    'name' : { 'en': 'Decrease' },
    'description' : 'Use this domain for words related to something decreasing in number or amount--to be less than before.',
    'value' : '8.1.4.3 Decrease'
  },
  {
    'guid' : 'f7960e84-5af9-4999-9028-783058aa8c5c',
    'id' : '8.1.5',
    'code' : '8.1.5',
    'abbr' : '8.1.5',
    'name' : { 'en': 'All' },
    'description' : 'Use this domain for words related to all.',
    'value' : '8.1.5 All'
  },
  {
    'guid' : 'cbcff912-e1c2-4d9b-9938-85d73e7e7265',
    'id' : '8.1.5.1',
    'code' : '8.1.5.1',
    'abbr' : '8.1.5.1',
    'name' : { 'en': 'Some' },
    'description' : 'Use this domain for words related to some--a number or amount of things or people when the number is not stated; an indefinite number or amount.',
    'value' : '8.1.5.1 Some'
  },
  {
    'guid' : 'bfbfb9b5-363d-4767-a5cb-1b11b348efd6',
    'id' : '8.1.5.2',
    'code' : '8.1.5.2',
    'abbr' : '8.1.5.2',
    'name' : { 'en': 'None, nothing' },
    'description' : 'Use this domain for words that nothing, no one, never, and nowhere.',
    'value' : '8.1.5.2 None, nothing'
  },
  {
    'guid' : 'bbb21324-089d-4368-a2ac-37c6bbfcbffc',
    'id' : '8.1.5.3',
    'code' : '8.1.5.3',
    'abbr' : '8.1.5.3',
    'name' : { 'en': 'Both' },
    'description' : 'Use this domain for words referring to both of two things.',
    'value' : '8.1.5.3 Both'
  },
  {
    'guid' : 'd1687857-0f1d-4098-affb-b283a6677b6b',
    'id' : '8.1.5.4',
    'code' : '8.1.5.4',
    'abbr' : '8.1.5.4',
    'name' : { 'en': 'Most, almost all' },
    'description' : 'Use this domain for words related to most--more than half and less than all of something.',
    'value' : '8.1.5.4 Most, almost all'
  },
  {
    'guid' : '0aaacffe-9b6c-49a7-bf68-c0f9ff3e120e',
    'id' : '8.1.5.5',
    'code' : '8.1.5.5',
    'abbr' : '8.1.5.5',
    'name' : { 'en': 'Most, least' },
    'description' : 'Use this domain for words related to the most--the largest number or amount; or the least--the smallest number or amount. Most/least may refer to the largest/smallest number possible or needed. If there are several groups being counted, most/least may refer to the largest/smallest group.',
    'value' : '8.1.5.5 Most, least'
  },
  {
    'guid' : 'afdd8b8e-9502-4d06-94ee-e79815b65750',
    'id' : '8.1.5.6',
    'code' : '8.1.5.6',
    'abbr' : '8.1.5.6',
    'name' : { 'en': 'Almost' },
    'description' : 'Use this domain for words related to a number or amount that is almost the same as another number.',
    'value' : '8.1.5.6 Almost'
  },
  {
    'guid' : 'e94a5cf3-1fd4-4b52-902f-bbf0ad6bac2b',
    'id' : '8.1.5.7',
    'code' : '8.1.5.7',
    'abbr' : '8.1.5.7',
    'name' : { 'en': 'Only' },
    'description' : 'Use this domain for words referring to only a particular number or amount of people or things--no more than one, or no more than a particular number or amount.',
    'value' : '8.1.5.7 Only'
  },
  {
    'guid' : '8f4c9266-a025-4b7a-bd67-fe0c043bf6f5',
    'id' : '8.1.5.8',
    'code' : '8.1.5.8',
    'abbr' : '8.1.5.8',
    'name' : { 'en': 'Exact' },
    'description' : 'Use this domain for words that indicate whether a number or amount is exact--not more and not less.',
    'value' : '8.1.5.8 Exact'
  },
  {
    'guid' : 'ca752706-1c9e-43e7-bd17-845c4736ccd8',
    'id' : '8.1.5.8.1',
    'code' : '8.1.5.8.1',
    'abbr' : '8.1.5.8.1',
    'name' : { 'en': 'Approximate' },
    'description' : 'Use this domain for words that indicate whether a number or amount is approximate.',
    'value' : '8.1.5.8.1 Approximate'
  },
  {
    'guid' : '3389561c-f264-48b9-b94c-86c33fc3c423',
    'id' : '8.1.5.9',
    'code' : '8.1.5.9',
    'abbr' : '8.1.5.9',
    'name' : { 'en': 'Average' },
    'description' : 'Use this domain for words related to an average number.',
    'value' : '8.1.5.9 Average'
  },
  {
    'guid' : '7cfc8b3c-ad67-4928-ae6a-74afd47ced89',
    'id' : '8.1.6',
    'code' : '8.1.6',
    'abbr' : '8.1.6',
    'name' : { 'en': 'Whole, complete' },
    'description' : 'Use this domain for words describing a whole thing--all of something with no parts missing.',
    'value' : '8.1.6 Whole, complete'
  },
  {
    'guid' : 'aadefee4-ed14-4753-952e-e945f2972e37',
    'id' : '8.1.6.1',
    'code' : '8.1.6.1',
    'abbr' : '8.1.6.1',
    'name' : { 'en': 'Part' },
    'description' : 'Many things have parts. Use this domain for words referring to a part of something, and for words that express the idea that something has parts, that something is a part of something, or that link the whole with a part.',
    'value' : '8.1.6.1 Part'
  },
  {
    'guid' : '1d34380d-61bf-4247-9145-ba318a14a97e',
    'id' : '8.1.6.2',
    'code' : '8.1.6.2',
    'abbr' : '8.1.6.2',
    'name' : { 'en': 'Piece' },
    'description' : 'Use this domain for words referring to a part of something that has been broken or cut off.',
    'value' : '8.1.6.2 Piece'
  },
  {
    'guid' : '286ee16c-a218-43d5-bbac-ab15f80c3fcf',
    'id' : '8.1.7',
    'code' : '8.1.7',
    'abbr' : '8.1.7',
    'name' : { 'en': 'Enough' },
    'description' : 'Use this domain for words related to being or having enough--to have as much of something or as many of something as you need or want.',
    'value' : '8.1.7 Enough'
  },
  {
    'guid' : '755a7462-1d87-48b0-939c-08be5b5ea002',
    'id' : '8.1.7.1',
    'code' : '8.1.7.1',
    'abbr' : '8.1.7.1',
    'name' : { 'en': 'Extra' },
    'description' : 'Use this domain for words related to having extra--to have more than enough or more than what you need.',
    'value' : '8.1.7.1 Extra'
  },
  {
    'guid' : '1621aac3-4ea9-4373-bf1b-40fce0ca7b5e',
    'id' : '8.1.7.2',
    'code' : '8.1.7.2',
    'abbr' : '8.1.7.2',
    'name' : { 'en': 'Lack' },
    'description' : 'Use this domain for words related to having insufficient--to not have enough.',
    'value' : '8.1.7.2 Lack'
  },
  {
    'guid' : 'd1b3d0f0-5319-4a6a-8a70-2179a8e76d22',
    'id' : '8.1.7.3',
    'code' : '8.1.7.3',
    'abbr' : '8.1.7.3',
    'name' : { 'en': 'Need' },
    'description' : 'Use this domain for words related to needing something for some purpose.',
    'value' : '8.1.7.3 Need'
  },
  {
    'guid' : '60b8dcfd-49a0-4ab4-82a7-2c5058b325ae',
    'id' : '8.1.7.4',
    'code' : '8.1.7.4',
    'abbr' : '8.1.7.4',
    'name' : { 'en': 'Remain, remainder' },
    'description' : 'Use this domain for words related to the remainder of something--the part or amount of something that remains behind after the other parts have been taken away. Something can be left because everything else has been used or eaten, or everything else has been destroyed or burned.',
    'value' : '8.1.7.4 Remain, remainder'
  },
  {
    'guid' : '8a81b9bc-9c66-4d57-a2d1-2e592604c4b1',
    'id' : '8.1.8',
    'code' : '8.1.8',
    'abbr' : '8.1.8',
    'name' : { 'en': 'Full' },
    'description' : 'Use this domain for words referring to a container being full of something.',
    'value' : '8.1.8 Full'
  },
  {
    'guid' : 'e9cafabe-f0f7-4142-a6f6-d1c94bdc4b5c',
    'id' : '8.1.8.1',
    'code' : '8.1.8.1',
    'abbr' : '8.1.8.1',
    'name' : { 'en': 'Empty' },
    'description' : 'Use this domain for words referring to being empty.',
    'value' : '8.1.8.1 Empty'
  },
  {
    'guid' : '8a0c5ed9-0041-4af5-a193-329e6c9f2717',
    'id' : '8.2',
    'code' : '8.2',
    'abbr' : '8.2',
    'name' : { 'en': 'Big' },
    'description' : 'Use this domain for words describing something that is big.',
    'value' : '8.2 Big'
  },
  {
    'guid' : 'b43fae8e-6b19-42ed-98cd-d363174b9cf8',
    'id' : '8.2.1',
    'code' : '8.2.1',
    'abbr' : '8.2.1',
    'name' : { 'en': 'Small' },
    'description' : 'Use this domain for words referring to being small.',
    'value' : '8.2.1 Small'
  },
  {
    'guid' : '7648040b-0aa5-4d9a-8f13-ffd066b81602',
    'id' : '8.2.2',
    'code' : '8.2.2',
    'abbr' : '8.2.2',
    'name' : { 'en': 'Long' },
    'description' : 'Use this domain for words related to being long. In many languages there is more than one system of measuring length. These systems may be used for different purposes or in different jobs. For instance measuring the length of an object may use different words than measuring the distance a person travels.',
    'value' : '8.2.2 Long'
  },
  {
    'guid' : '3ccc3a21-07c8-4983-a044-e3c74b538135',
    'id' : '8.2.2.1',
    'code' : '8.2.2.1',
    'abbr' : '8.2.2.1',
    'name' : { 'en': 'Short, not long' },
    'description' : 'Use this domain for words related to being short in length.',
    'value' : '8.2.2.1 Short, not long'
  },
  {
    'guid' : 'b844d2f8-d3ef-4605-b038-8bc0a2cff0af',
    'id' : '8.2.2.2',
    'code' : '8.2.2.2',
    'abbr' : '8.2.2.2',
    'name' : { 'en': 'Tall' },
    'description' : 'Use this domain for words related to being tall--a word describing something that is big from the top to the bottom.',
    'value' : '8.2.2.2 Tall'
  },
  {
    'guid' : '0aae1951-4d5b-45a0-853c-1839764c9862',
    'id' : '8.2.2.3',
    'code' : '8.2.2.3',
    'abbr' : '8.2.2.3',
    'name' : { 'en': 'Short, not tall' },
    'description' : 'Use this domain for words related to being short in height.',
    'value' : '8.2.2.3 Short, not tall'
  },
  {
    'guid' : 'b1688009-474d-4e2e-a137-acc1e32a435f',
    'id' : '8.2.3',
    'code' : '8.2.3',
    'abbr' : '8.2.3',
    'name' : { 'en': 'Thick' },
    'description' : 'Use this domain for words referring to being thick.',
    'value' : '8.2.3 Thick'
  },
  {
    'guid' : '627280f0-4f98-4b31-ad23-eabe37b002ad',
    'id' : '8.2.3.1',
    'code' : '8.2.3.1',
    'abbr' : '8.2.3.1',
    'name' : { 'en': 'Thin thing' },
    'description' : 'Use this domain for words describing something that is thin.',
    'value' : '8.2.3.1 Thin thing'
  },
  {
    'guid' : '45d867c7-8496-4c92-bb41-b7db5db47717',
    'id' : '8.2.3.2',
    'code' : '8.2.3.2',
    'abbr' : '8.2.3.2',
    'name' : { 'en': 'Fat person' },
    'description' : 'Use this domain for words describing a person or animal who is fat.',
    'value' : '8.2.3.2 Fat person'
  },
  {
    'guid' : '08c05e00-9660-4491-af2f-a05fab27ef39',
    'id' : '8.2.3.3',
    'code' : '8.2.3.3',
    'abbr' : '8.2.3.3',
    'name' : { 'en': 'Thin person' },
    'description' : 'Use this domain for words describing a person or animal who is thin.',
    'value' : '8.2.3.3 Thin person'
  },
  {
    'guid' : 'f8863b67-b911-4334-a1b6-6eb913bd14af',
    'id' : '8.2.4',
    'code' : '8.2.4',
    'abbr' : '8.2.4',
    'name' : { 'en': 'Wide' },
    'description' : 'Use this domain for words related to being wide--a word describing something like a road or river that is far from one side to the other side.',
    'value' : '8.2.4 Wide'
  },
  {
    'guid' : 'aa2b547c-2c82-4ce0-a1b3-35fa809d666c',
    'id' : '8.2.4.1',
    'code' : '8.2.4.1',
    'abbr' : '8.2.4.1',
    'name' : { 'en': 'Narrow' },
    'description' : 'Use this domain for words related to being narrow.',
    'value' : '8.2.4.1 Narrow'
  },
  {
    'guid' : '67e57493-d286-4271-b877-f63f962dddf1',
    'id' : '8.2.5',
    'code' : '8.2.5',
    'abbr' : '8.2.5',
    'name' : { 'en': 'Big area' },
    'description' : 'Use this domain for words describing a big area.',
    'value' : '8.2.5 Big area'
  },
  {
    'guid' : 'ea17aba7-6d4e-4dbf-89ea-84a1b1c47647',
    'id' : '8.2.5.1',
    'code' : '8.2.5.1',
    'abbr' : '8.2.5.1',
    'name' : { 'en': 'Big container, volume' },
    'description' : 'Use this domain for words referring to the volume of something.',
    'value' : '8.2.5.1 Big container, volume'
  },
  {
    'guid' : '73d580ac-dc89-474c-8048-3453ebdda807',
    'id' : '8.2.6',
    'code' : '8.2.6',
    'abbr' : '8.2.6',
    'name' : { 'en': 'Distance' },
    'description' : 'Use this domain for words referring to the distance between two things.',
    'value' : '8.2.6 Distance'
  },
  {
    'guid' : 'bba30b56-6cd8-4542-81ab-f983cf1354bd',
    'id' : '8.2.6.1',
    'code' : '8.2.6.1',
    'abbr' : '8.2.6.1',
    'name' : { 'en': 'Far' },
    'description' : 'Use this domain for words indicating that something is far from something else.',
    'value' : '8.2.6.1 Far'
  },
  {
    'guid' : '1133ad78-9ce9-46aa-b181-bb6f7a84a07b',
    'id' : '8.2.6.2',
    'code' : '8.2.6.2',
    'abbr' : '8.2.6.2',
    'name' : { 'en': 'Near' },
    'description' : 'Use this domain for words indicating that something is near something else.',
    'value' : '8.2.6.2 Near'
  },
  {
    'guid' : '7d8898d6-6296-4d4d-b8dd-2f48c49f9e98',
    'id' : '8.2.6.3',
    'code' : '8.2.6.3',
    'abbr' : '8.2.6.3',
    'name' : { 'en': 'High' },
    'description' : 'Use this domain for words that express the idea that something is high.',
    'value' : '8.2.6.3 High'
  },
  {
    'guid' : '6c54f0b0-b056-4090-b3f0-d5ec6710d4ab',
    'id' : '8.2.6.4',
    'code' : '8.2.6.4',
    'abbr' : '8.2.6.4',
    'name' : { 'en': 'Low' },
    'description' : 'Use this domain for words indicating that something is low--in the air but not high above the ground.',
    'value' : '8.2.6.4 Low'
  },
  {
    'guid' : '4e7a6dfe-3654-4ca1-874d-02424581b774',
    'id' : '8.2.6.5',
    'code' : '8.2.6.5',
    'abbr' : '8.2.6.5',
    'name' : { 'en': 'Deep, shallow' },
    'description' : 'Use this domain for words related to being deep or shallow--how far something such as a hole extends below the ground or other surface, or how far something is below the surface of the water.',
    'value' : '8.2.6.5 Deep, shallow'
  },
  {
    'guid' : 'd0dee676-f3ae-43cc-96f1-7e3bb65870f5',
    'id' : '8.2.7',
    'code' : '8.2.7',
    'abbr' : '8.2.7',
    'name' : { 'en': 'Fit, size' },
    'description' : 'Use this domain for words referring to something fitting--when something is not too big or too small, but just right.',
    'value' : '8.2.7 Fit, size'
  },
  {
    'guid' : '4d61f524-7213-4c2c-8c14-f8eff3aed813',
    'id' : '8.2.7.1',
    'code' : '8.2.7.1',
    'abbr' : '8.2.7.1',
    'name' : { 'en': 'Tight' },
    'description' : 'Use this domain for words referring to being tight--when something is too small.',
    'value' : '8.2.7.1 Tight'
  },
  {
    'guid' : '7162885d-1d35-4baf-97d6-7368fff7c723',
    'id' : '8.2.7.2',
    'code' : '8.2.7.2',
    'abbr' : '8.2.7.2',
    'name' : { 'en': 'Loose' },
    'description' : 'Use this domain for words referring to being loose--when something is too big.',
    'value' : '8.2.7.2 Loose'
  },
  {
    'guid' : 'a3fb18fd-befb-493f-8a19-760883ed9697',
    'id' : '8.2.7.3',
    'code' : '8.2.7.3',
    'abbr' : '8.2.7.3',
    'name' : { 'en': 'Wedged in, stuck' },
    'description' : 'Use this domain for words referring to being wedged in or stuck in a hole.',
    'value' : '8.2.7.3 Wedged in, stuck'
  },
  {
    'guid' : 'dfdcfa24-b013-4566-af4a-28ef1dfd4742',
    'id' : '8.2.8',
    'code' : '8.2.8',
    'abbr' : '8.2.8',
    'name' : { 'en': 'Measure' },
    'description' : 'Use this domain for words related to measuring something--to find out the size, length, weight, or amount of something.',
    'value' : '8.2.8 Measure'
  },
  {
    'guid' : 'aba3f7f1-9e13-4b48-acbb-3bf6d6bfa0e8',
    'id' : '8.2.9',
    'code' : '8.2.9',
    'abbr' : '8.2.9',
    'name' : { 'en': 'Weigh' },
    'description' : 'Use this domain for words related to weighing something.',
    'value' : '8.2.9 Weigh'
  },
  {
    'guid' : 'd574c970-6834-4566-ae37-f42c7e95483b',
    'id' : '8.2.9.1',
    'code' : '8.2.9.1',
    'abbr' : '8.2.9.1',
    'name' : { 'en': 'Heavy' },
    'description' : 'Use this domain for words related to being heavy.',
    'value' : '8.2.9.1 Heavy'
  },
  {
    'guid' : '6e7d10f8-6da5-4a8a-a06d-952511194105',
    'id' : '8.2.9.2',
    'code' : '8.2.9.2',
    'abbr' : '8.2.9.2',
    'name' : { 'en': 'Light in weight' },
    'description' : 'Use this domain for words related to being light in weight.',
    'value' : '8.2.9.2 Light in weight'
  },
  {
    'guid' : 'ac9ee84f-f0c7-48b3-8e5a-b4c967112394',
    'id' : '8.3',
    'code' : '8.3',
    'abbr' : '8.3',
    'name' : { 'en': 'Quality' },
    'description' : 'Use this domain for general words referring to the quality or condition of something.',
    'value' : '8.3 Quality'
  },
  {
    'guid' : '080bf07b-e58b-4a75-bb97-84d980a143f0',
    'id' : '8.3.1',
    'code' : '8.3.1',
    'abbr' : '8.3.1',
    'name' : { 'en': 'Shape' },
    'description' : 'Use this domain for general words referring to the shape of something, and for general words referring to changing the shape of something.',
    'value' : '8.3.1 Shape'
  },
  {
    'guid' : '6ffe33fe-b49c-45c8-a50b-cd0065c0c869',
    'id' : '8.3.1.1',
    'code' : '8.3.1.1',
    'abbr' : '8.3.1.1',
    'name' : { 'en': 'Point, dot' },
    'description' : 'Use this domain for words referring to a point--a small mark such as might be made by a pointed object.',
    'value' : '8.3.1.1 Point, dot'
  },
  {
    'guid' : '07e97f87-68ca-4d18-9f86-a326e0400947',
    'id' : '8.3.1.2',
    'code' : '8.3.1.2',
    'abbr' : '8.3.1.2',
    'name' : { 'en': 'Line' },
    'description' : 'Use this domain for words referring to a line.',
    'value' : '8.3.1.2 Line'
  },
  {
    'guid' : 'a3ca2a31-259e-4e15-9696-75b0c81886e9',
    'id' : '8.3.1.3',
    'code' : '8.3.1.3',
    'abbr' : '8.3.1.3',
    'name' : { 'en': 'Straight' },
    'description' : 'Use this domain for words referring to being straight.',
    'value' : '8.3.1.3 Straight'
  },
  {
    'guid' : '61f40376-729d-4d99-894f-06c5689a06ac',
    'id' : '8.3.1.3.1',
    'code' : '8.3.1.3.1',
    'abbr' : '8.3.1.3.1',
    'name' : { 'en': 'Flat' },
    'description' : 'Use this domain for words describing something that is flat--having a surface that is even. A board or wall is flat when its surface is even and it does not bend.',
    'value' : '8.3.1.3.1 Flat'
  },
  {
    'guid' : '82a4ae36-8e70-4c0d-8144-ad9fc3c0e04f',
    'id' : '8.3.1.4',
    'code' : '8.3.1.4',
    'abbr' : '8.3.1.4',
    'name' : { 'en': 'Horizontal' },
    'description' : 'Use this domain for words describing a horizontal orientation in relation to the ground or something that is level--a flat surface that does not rise in any direction. A person is horizontal when he is sleeping. A field is level when it is not on a hill and it has no uneven areas in it.',
    'value' : '8.3.1.4 Horizontal'
  },
  {
    'guid' : 'b8df8589-d03c-4e6d-bbeb-4f24fbf6a1dc',
    'id' : '8.3.1.4.1',
    'code' : '8.3.1.4.1',
    'abbr' : '8.3.1.4.1',
    'name' : { 'en': 'Vertical' },
    'description' : 'Use this domain for words describing a vertical orientation in relation to the ground. A person is vertical when he is standing.',
    'value' : '8.3.1.4.1 Vertical'
  },
  {
    'guid' : '0add0775-0ed0-46be-ba4a-76310e63a036',
    'id' : '8.3.1.4.2',
    'code' : '8.3.1.4.2',
    'abbr' : '8.3.1.4.2',
    'name' : { 'en': 'Leaning, sloping' },
    'description' : 'Use this domain for words describing a leaning orientation in relation to the ground, or a surface that is sloping.',
    'value' : '8.3.1.4.2 Leaning, sloping'
  },
  {
    'guid' : '5c35c2f8-d17c-42a3-aa12-a4c29b6603e8',
    'id' : '8.3.1.5',
    'code' : '8.3.1.5',
    'abbr' : '8.3.1.5',
    'name' : { 'en': 'Bend' },
    'description' : 'Use this domain for words referring to bending something and for words that describe something that is bent or curved.',
    'value' : '8.3.1.5 Bend'
  },
  {
    'guid' : '7ee92ca4-19aa-4abd-9f88-508766acc39c',
    'id' : '8.3.1.5.1',
    'code' : '8.3.1.5.1',
    'abbr' : '8.3.1.5.1',
    'name' : { 'en': 'Roll up' },
    'description' : 'Use this domain for words referring to rolling something up.',
    'value' : '8.3.1.5.1 Roll up'
  },
  {
    'guid' : '19fea936-30d1-482f-a103-1c5549b19745',
    'id' : '8.3.1.5.2',
    'code' : '8.3.1.5.2',
    'abbr' : '8.3.1.5.2',
    'name' : { 'en': 'Twist, wring' },
    'description' : 'Use this domain for words referring to twisting something--to take something long and turn one end one way and the other end the other way.',
    'value' : '8.3.1.5.2 Twist, wring'
  },
  {
    'guid' : '8f7c9d7d-9b2a-40f3-9314-8f16f0aa31ef',
    'id' : '8.3.1.5.3',
    'code' : '8.3.1.5.3',
    'abbr' : '8.3.1.5.3',
    'name' : { 'en': 'Fold' },
    'description' : 'Use this domain for words referring to folding something.',
    'value' : '8.3.1.5.3 Fold'
  },
  {
    'guid' : '6c36a680-4ff4-43b9-a7c2-9037ceb0d3f6',
    'id' : '8.3.1.6',
    'code' : '8.3.1.6',
    'abbr' : '8.3.1.6',
    'name' : { 'en': 'Round' },
    'description' : 'Use this domain for words referring to being round.',
    'value' : '8.3.1.6 Round'
  },
  {
    'guid' : '2d563d27-8ac3-41c9-b326-856c9e1f6401',
    'id' : '8.3.1.6.1',
    'code' : '8.3.1.6.1',
    'abbr' : '8.3.1.6.1',
    'name' : { 'en': 'Concave' },
    'description' : 'Use this domain for words describing something that is concave--extending inward in shape away from the viewer. The inside of a bowl is concave in shape.',
    'value' : '8.3.1.6.1 Concave'
  },
  {
    'guid' : '995ee828-2393-462b-be82-47f5b5439aaf',
    'id' : '8.3.1.6.2',
    'code' : '8.3.1.6.2',
    'abbr' : '8.3.1.6.2',
    'name' : { 'en': 'Convex' },
    'description' : 'Use this domain for words describing something that is convex--extending outward in shape toward the viewer.',
    'value' : '8.3.1.6.2 Convex'
  },
  {
    'guid' : '3d10e03a-7902-458d-9c45-938da103d639',
    'id' : '8.3.1.6.3',
    'code' : '8.3.1.6.3',
    'abbr' : '8.3.1.6.3',
    'name' : { 'en': 'Hollow' },
    'description' : 'Use this domain for words describing something that is hollow--empty on the inside.',
    'value' : '8.3.1.6.3 Hollow'
  },
  {
    'guid' : '0fabc72a-ce97-41f3-8a2d-2f27eae09499',
    'id' : '8.3.1.7',
    'code' : '8.3.1.7',
    'abbr' : '8.3.1.7',
    'name' : { 'en': 'Square' },
    'description' : 'Use this domain for words referring to being square.',
    'value' : '8.3.1.7 Square'
  },
  {
    'guid' : '706cb38c-9aca-4e2f-9653-d9562f07331c',
    'id' : '8.3.1.8',
    'code' : '8.3.1.8',
    'abbr' : '8.3.1.8',
    'name' : { 'en': 'Pattern, design' },
    'description' : 'Use this domain for words that refer to a pattern--a regular arrangement of shapes.',
    'value' : '8.3.1.8 Pattern, design'
  },
  {
    'guid' : '45b7dcce-21d5-4738-a64d-e8b0be8a1824',
    'id' : '8.3.1.8.1',
    'code' : '8.3.1.8.1',
    'abbr' : '8.3.1.8.1',
    'name' : { 'en': 'Symmetrical' },
    'description' : 'Use this domain for words that describe something that is symmetrical--having the same shape on both sides',
    'value' : '8.3.1.8.1 Symmetrical'
  },
  {
    'guid' : '2594fe01-4d20-4a20-b093-2df70bced18f',
    'id' : '8.3.1.9',
    'code' : '8.3.1.9',
    'abbr' : '8.3.1.9',
    'name' : { 'en': 'Stretch' },
    'description' : 'Use this domain for words referring to stretching something.',
    'value' : '8.3.1.9 Stretch'
  },
  {
    'guid' : '2621e605-3ecc-4f3d-b28c-f8c92b3c4584',
    'id' : '8.3.2',
    'code' : '8.3.2',
    'abbr' : '8.3.2',
    'name' : { 'en': 'Texture' },
    'description' : 'Use this domain for general words referring to the texture of something--how the surface of something feels when you touch it.',
    'value' : '8.3.2 Texture'
  },
  {
    'guid' : '867f515b-ed0f-431e-a84a-6c562e1bdbb7',
    'id' : '8.3.2.1',
    'code' : '8.3.2.1',
    'abbr' : '8.3.2.1',
    'name' : { 'en': 'Smooth' },
    'description' : 'Use this domain for words referring to being smooth.',
    'value' : '8.3.2.1 Smooth'
  },
  {
    'guid' : '2e09535f-f61f-4ff5-8d56-23c2916cbb7f',
    'id' : '8.3.2.2',
    'code' : '8.3.2.2',
    'abbr' : '8.3.2.2',
    'name' : { 'en': 'Rough' },
    'description' : 'Use this domain for words referring to being rough.',
    'value' : '8.3.2.2 Rough'
  },
  {
    'guid' : '313ca832-ce91-44c9-bb35-bd130c39d924',
    'id' : '8.3.2.3',
    'code' : '8.3.2.3',
    'abbr' : '8.3.2.3',
    'name' : { 'en': 'Sharp' },
    'description' : 'Use this domain for words describing something that is sharp.',
    'value' : '8.3.2.3 Sharp'
  },
  {
    'guid' : '34fe8676-7bda-493d-a012-bc5748e87823',
    'id' : '8.3.2.3.1',
    'code' : '8.3.2.3.1',
    'abbr' : '8.3.2.3.1',
    'name' : { 'en': 'Pointed' },
    'description' : 'Use this domain for words describing something that is pointed.',
    'value' : '8.3.2.3.1 Pointed'
  },
  {
    'guid' : '396a2a1b-832f-4180-b26a-c606550541d7',
    'id' : '8.3.2.4',
    'code' : '8.3.2.4',
    'abbr' : '8.3.2.4',
    'name' : { 'en': 'Blunt' },
    'description' : 'Use this domain for words describing something that is blunt.',
    'value' : '8.3.2.4 Blunt'
  },
  {
    'guid' : 'c8aea8b2-4088-4d20-a0d2-45c2ad974ee1',
    'id' : '8.3.2.5',
    'code' : '8.3.2.5',
    'abbr' : '8.3.2.5',
    'name' : { 'en': 'Furrow' },
    'description' : 'Use this domain for words related to a furrow--a long mark cut into the surface of something, such as the furrow made by a plow, or a long cut made by a knife.',
    'value' : '8.3.2.5 Furrow'
  },
  {
    'guid' : '4bf411b7-2b5b-4673-b116-0e6c31fbd08a',
    'id' : '8.3.3',
    'code' : '8.3.3',
    'abbr' : '8.3.3',
    'name' : { 'en': 'Light' },
    'description' : 'Use this domain for words related to light.',
    'value' : '8.3.3 Light'
  },
  {
    'guid' : 'a7824686-a3f3-4c8a-907e-5d841cf846c8',
    'id' : '8.3.3.1',
    'code' : '8.3.3.1',
    'abbr' : '8.3.3.1',
    'name' : { 'en': 'Shine' },
    'description' : 'Use this domain for words related to a light source shining--for something to make light.',
    'value' : '8.3.3.1 Shine'
  },
  {
    'guid' : '64e41545-3a0a-4524-a8d4-8e5e0f0e2391',
    'id' : '8.3.3.1.1',
    'code' : '8.3.3.1.1',
    'abbr' : '8.3.3.1.1',
    'name' : { 'en': 'Light source' },
    'description' : 'Use this domain for words related to a source of light--something that makes or gives light.',
    'value' : '8.3.3.1.1 Light source'
  },
  {
    'guid' : '2330813b-7413-41a8-8eb2-ae138511c953',
    'id' : '8.3.3.1.2',
    'code' : '8.3.3.1.2',
    'abbr' : '8.3.3.1.2',
    'name' : { 'en': 'Bright' },
    'description' : 'Use this domain for words describing something that is bright.',
    'value' : '8.3.3.1.2 Bright'
  },
  {
    'guid' : 'bf9606ec-9a8e-4822-8bfd-d5eebc58c65b',
    'id' : '8.3.3.2',
    'code' : '8.3.3.2',
    'abbr' : '8.3.3.2',
    'name' : { 'en': 'Dark' },
    'description' : 'Use this domain for words describing something that is dark--a place where there is little or no light.',
    'value' : '8.3.3.2 Dark'
  },
  {
    'guid' : 'cba6876c-5b48-42f4-ae0a-7fbe9bb971ef',
    'id' : '8.3.3.2.1',
    'code' : '8.3.3.2.1',
    'abbr' : '8.3.3.2.1',
    'name' : { 'en': 'Shadow' },
    'description' : 'Use this domain for words related to a shadow--the area on the ground where the light does not shine because something is in the way. For instance if the sun\n(or another light) is shining on an object, the area behind the object is in shadow (dark).',
    'value' : '8.3.3.2.1 Shadow'
  },
  {
    'guid' : 'd01f1c51-522e-4b35-81b3-00577dbfa3bd',
    'id' : '8.3.3.3',
    'code' : '8.3.3.3',
    'abbr' : '8.3.3.3',
    'name' : { 'en': 'Color' },
    'description' : 'Use this domain for words related to color.',
    'value' : '8.3.3.3 Color'
  },
  {
    'guid' : '7adf468c-b93a-4b04-8af6-4c691122a4eb',
    'id' : '8.3.3.3.1',
    'code' : '8.3.3.3.1',
    'abbr' : '8.3.3.3.1',
    'name' : { 'en': 'White' },
    'description' : 'Use this domain for words describing something that is white.',
    'value' : '8.3.3.3.1 White'
  },
  {
    'guid' : 'ff73fb69-7dac-43e2-876b-0ead264c3f2d',
    'id' : '8.3.3.3.2',
    'code' : '8.3.3.3.2',
    'abbr' : '8.3.3.3.2',
    'name' : { 'en': 'Black' },
    'description' : 'Use this domain for words describing something that is black.',
    'value' : '8.3.3.3.2 Black'
  },
  {
    'guid' : '538a4c20-01d7-40b9-b462-ae279ff3dc27',
    'id' : '8.3.3.3.3',
    'code' : '8.3.3.3.3',
    'abbr' : '8.3.3.3.3',
    'name' : { 'en': 'Gray' },
    'description' : 'Use this domain for words describing something that is gray.',
    'value' : '8.3.3.3.3 Gray'
  },
  {
    'guid' : '536a2d3e-2303-43bc-bf53-379131eb5730',
    'id' : '8.3.3.3.4',
    'code' : '8.3.3.3.4',
    'abbr' : '8.3.3.3.4',
    'name' : { 'en': 'Colors of the spectrum' },
    'description' : 'Use this domain for words describing something that is colored.',
    'value' : '8.3.3.3.4 Colors of the spectrum'
  },
  {
    'guid' : '16de6eab-afab-4ba4-a279-cf0ba4d7c9e6',
    'id' : '8.3.3.3.5',
    'code' : '8.3.3.3.5',
    'abbr' : '8.3.3.3.5',
    'name' : { 'en': 'Animal color, marking' },
    'description' : 'Use this domain for words related to animal colors and markings.',
    'value' : '8.3.3.3.5 Animal color, marking'
  },
  {
    'guid' : 'a8b3fa0c-077e-4c0d-b4cc-36dcfbdcb4d4',
    'id' : '8.3.3.3.6',
    'code' : '8.3.3.3.6',
    'abbr' : '8.3.3.3.6',
    'name' : { 'en': 'Change color' },
    'description' : 'Use this domain for words related to changing the color of something.',
    'value' : '8.3.3.3.6 Change color'
  },
  {
    'guid' : '97ed5af8-29ca-428d-8ac5-c61b61a963fd',
    'id' : '8.3.3.3.7',
    'code' : '8.3.3.3.7',
    'abbr' : '8.3.3.3.7',
    'name' : { 'en': 'Multicolored' },
    'description' : 'Use this domain for words describing something that is multicolored--having many different colors.',
    'value' : '8.3.3.3.7 Multicolored'
  },
  {
    'guid' : 'aed6c1ec-abbe-47e3-a6cc-a99ecff3b825',
    'id' : '8.3.3.4',
    'code' : '8.3.3.4',
    'abbr' : '8.3.3.4',
    'name' : { 'en': 'Shiny' },
    'description' : 'Use this domain for words that describe something that is shiny--when something gives back light because light is shining on it.',
    'value' : '8.3.3.4 Shiny'
  },
  {
    'guid' : '742996cd-b87f-40a8-bdb3-dba74219bd73',
    'id' : '8.3.4',
    'code' : '8.3.4',
    'abbr' : '8.3.4',
    'name' : { 'en': 'Hot' },
    'description' : 'Use this domain for words describing something that is hot.',
    'value' : '8.3.4 Hot'
  },
  {
    'guid' : '82e4394a-2f58-4356-8d9c-1ad9dbe95293',
    'id' : '8.3.4.1',
    'code' : '8.3.4.1',
    'abbr' : '8.3.4.1',
    'name' : { 'en': 'Cold' },
    'description' : 'Use this domain for words describing something that is cold.',
    'value' : '8.3.4.1 Cold'
  },
  {
    'guid' : 'bfeba2a4-4479-49e9-838c-3baa2ad0fcae',
    'id' : '8.3.5',
    'code' : '8.3.5',
    'abbr' : '8.3.5',
    'name' : { 'en': 'Type, kind' },
    'description' : 'Use this domain for words related to something being a type of thing, or that something belongs to a class of things.',
    'value' : '8.3.5 Type, kind'
  },
  {
    'guid' : '25763563-5ad6-4b4d-9073-3fc88f6dd44e',
    'id' : '8.3.5.1',
    'code' : '8.3.5.1',
    'abbr' : '8.3.5.1',
    'name' : { 'en': 'Nature, character' },
    'description' : 'Use this domain for words related to the nature of character of something.',
    'value' : '8.3.5.1 Nature, character'
  },
  {
    'guid' : 'fe9253f4-d063-4d63-91af-85273d61337f',
    'id' : '8.3.5.2',
    'code' : '8.3.5.2',
    'abbr' : '8.3.5.2',
    'name' : { 'en': 'Compare' },
    'description' : 'Use this domain for words related to comparing something or someone with another thing.',
    'value' : '8.3.5.2 Compare'
  },
  {
    'guid' : '99b5b80a-a2d6-4820-adfc-1da72528c272',
    'id' : '8.3.5.2.1',
    'code' : '8.3.5.2.1',
    'abbr' : '8.3.5.2.1',
    'name' : { 'en': 'Same' },
    'description' : 'Use this domain for words describing something that is the same thing as you just mentioned, or describing two things that are exactly the same.',
    'value' : '8.3.5.2.1 Same'
  },
  {
    'guid' : 'fba27833-d6f1-4c36-ac39-28902b29261b',
    'id' : '8.3.5.2.2',
    'code' : '8.3.5.2.2',
    'abbr' : '8.3.5.2.2',
    'name' : { 'en': 'Like, similar' },
    'description' : 'Use this domain for words describing two things or people that are similar, but not the same.',
    'value' : '8.3.5.2.2 Like, similar'
  },
  {
    'guid' : '0d427d55-d63e-4a35-a66a-5e4dce0a963e',
    'id' : '8.3.5.2.3',
    'code' : '8.3.5.2.3',
    'abbr' : '8.3.5.2.3',
    'name' : { 'en': 'Different' },
    'description' : 'Use this domain for words describing two things that are different--not the same.',
    'value' : '8.3.5.2.3 Different'
  },
  {
    'guid' : '99988f94-3aa4-4984-88df-ae228f01d3b7',
    'id' : '8.3.5.2.4',
    'code' : '8.3.5.2.4',
    'abbr' : '8.3.5.2.4',
    'name' : { 'en': 'Other' },
    'description' : 'Use this domain for words related to other, as in "the other person", "another thing"--a thing that is not the same as something that has been mentioned.',
    'value' : '8.3.5.2.4 Other'
  },
  {
    'guid' : '2f98291a-47a7-4b7b-9256-2c0249105be1',
    'id' : '8.3.5.2.5',
    'code' : '8.3.5.2.5',
    'abbr' : '8.3.5.2.5',
    'name' : { 'en': 'Various' },
    'description' : 'Use this domain for words describing a group of things that are all different from each other.',
    'value' : '8.3.5.2.5 Various'
  },
  {
    'guid' : '23fa2115-3979-472c-8939-4db8d54e4c98',
    'id' : '8.3.5.2.6',
    'code' : '8.3.5.2.6',
    'abbr' : '8.3.5.2.6',
    'name' : { 'en': 'Opposite' },
    'description' : 'Use this domain for words describing two things that are opposite.',
    'value' : '8.3.5.2.6 Opposite'
  },
  {
    'guid' : '06a44085-cbcf-4217-ae5e-56c51899c99a',
    'id' : '8.3.5.3',
    'code' : '8.3.5.3',
    'abbr' : '8.3.5.3',
    'name' : { 'en': 'Common' },
    'description' : 'Use this domain for words describing something that is common.',
    'value' : '8.3.5.3 Common'
  },
  {
    'guid' : 'f6896060-4d5c-45e2-b89a-f9f6328a479c',
    'id' : '8.3.5.3.1',
    'code' : '8.3.5.3.1',
    'abbr' : '8.3.5.3.1',
    'name' : { 'en': 'Usual' },
    'description' : 'Use this domain for words describing something that is usual.',
    'value' : '8.3.5.3.1 Usual'
  },
  {
    'guid' : 'b0156a7c-928a-4cc8-a021-4af4dc74fead',
    'id' : '8.3.5.3.2',
    'code' : '8.3.5.3.2',
    'abbr' : '8.3.5.3.2',
    'name' : { 'en': 'Unusual' },
    'description' : 'Use this domain for words describing something that is unusual.',
    'value' : '8.3.5.3.2 Unusual'
  },
  {
    'guid' : '2cccfd92-de45-42c2-83f7-1e0ef7dfddc1',
    'id' : '8.3.5.3.3',
    'code' : '8.3.5.3.3',
    'abbr' : '8.3.5.3.3',
    'name' : { 'en': 'Unique' },
    'description' : 'Use this domain for words describing something that is unique--not like anything else.',
    'value' : '8.3.5.3.3 Unique'
  },
  {
    'guid' : '6a6f0748-a6e9-4d64-b14e-543bb6ec2ec8',
    'id' : '8.3.5.3.4',
    'code' : '8.3.5.3.4',
    'abbr' : '8.3.5.3.4',
    'name' : { 'en': 'Strange' },
    'description' : 'Use this domain for words describing something that is strange.',
    'value' : '8.3.5.3.4 Strange'
  },
  {
    'guid' : 'ac527685-f31d-42f3-81bd-97221a01c7ef',
    'id' : '8.3.5.4',
    'code' : '8.3.5.4',
    'abbr' : '8.3.5.4',
    'name' : { 'en': 'Pattern, model' },
    'description' : 'Use this domain for words related to a pattern or model.',
    'value' : '8.3.5.4 Pattern, model'
  },
  {
    'guid' : 'af62c8f6-43c7-4c44-a0d1-ab9bcff8e26f',
    'id' : '8.3.5.5',
    'code' : '8.3.5.5',
    'abbr' : '8.3.5.5',
    'name' : { 'en': 'Imitate' },
    'description' : 'Use this domain for words related to imitating someone--to do things in the same way as another person.',
    'value' : '8.3.5.5 Imitate'
  },
  {
    'guid' : '0c7c33f2-4cfa-42df-84bb-19fc915a72bd',
    'id' : '8.3.5.6',
    'code' : '8.3.5.6',
    'abbr' : '8.3.5.6',
    'name' : { 'en': 'Copy' },
    'description' : 'Use this domain for words related to copying something.',
    'value' : '8.3.5.6 Copy'
  },
  {
    'guid' : 'c05bf8c3-78f2-4ec5-b0d7-32d4963a5794',
    'id' : '8.3.6',
    'code' : '8.3.6',
    'abbr' : '8.3.6',
    'name' : { 'en': 'Made of, material' },
    'description' : 'Use this domain for words that mark the material out of which something has been made.',
    'value' : '8.3.6 Made of, material'
  },
  {
    'guid' : 'e86364ac-6fa1-4aad-a6c1-068d56b6a1f1',
    'id' : '8.3.6.1',
    'code' : '8.3.6.1',
    'abbr' : '8.3.6.1',
    'name' : { 'en': 'Strong, brittle' },
    'description' : 'Use this domain for words that describe something that is strong--not easily broken.',
    'value' : '8.3.6.1 Strong, brittle'
  },
  {
    'guid' : '16ee1e09-27ad-48c5-aa07-6933ecbbc716',
    'id' : '8.3.6.2',
    'code' : '8.3.6.2',
    'abbr' : '8.3.6.2',
    'name' : { 'en': 'Hard, firm' },
    'description' : 'Use this domain for words that describe something that is hard--not easily cut, or broken.',
    'value' : '8.3.6.2 Hard, firm'
  },
  {
    'guid' : '314c8fea-4bdb-4bc8-ab67-a26a9c5abbd4',
    'id' : '8.3.6.3',
    'code' : '8.3.6.3',
    'abbr' : '8.3.6.3',
    'name' : { 'en': 'Stiff, flexible' },
    'description' : 'Use this domain for words that describe something that is stiff--not easy to bend.',
    'value' : '8.3.6.3 Stiff, flexible'
  },
  {
    'guid' : '6c305af5-cff5-4f7f-bd89-040d4c265355',
    'id' : '8.3.6.4',
    'code' : '8.3.6.4',
    'abbr' : '8.3.6.4',
    'name' : { 'en': 'Dense' },
    'description' : 'Use this domain for words describing something that is dense.',
    'value' : '8.3.6.4 Dense'
  },
  {
    'guid' : '47f170eb-5f1d-49a5-85bb-240047f392c0',
    'id' : '8.3.6.5',
    'code' : '8.3.6.5',
    'abbr' : '8.3.6.5',
    'name' : { 'en': 'Soft, flimsy' },
    'description' : 'Use this domain for words describing something that is soft or flimsy.',
    'value' : '8.3.6.5 Soft, flimsy'
  },
  {
    'guid' : '83adeb9d-c0be-4073-894d-913014420280',
    'id' : '8.3.7',
    'code' : '8.3.7',
    'abbr' : '8.3.7',
    'name' : { 'en': 'Good' },
    'description' : 'Use this domain for words describing something that is good.',
    'value' : '8.3.7 Good'
  },
  {
    'guid' : 'aecf2aad-b7a4-444f-9b13-64bc534126d2',
    'id' : '8.3.7.1',
    'code' : '8.3.7.1',
    'abbr' : '8.3.7.1',
    'name' : { 'en': 'Bad' },
    'description' : 'Use this domain for words describing something bad.',
    'value' : '8.3.7.1 Bad'
  },
  {
    'guid' : '434ec34f-e7ca-44f8-9252-dff5b9b2b62f',
    'id' : '8.3.7.2',
    'code' : '8.3.7.2',
    'abbr' : '8.3.7.2',
    'name' : { 'en': 'Better' },
    'description' : 'Use this domain for words describing something that is better than something else.',
    'value' : '8.3.7.2 Better'
  },
  {
    'guid' : '91cc7e8f-522e-4ff1-b545-5a5b72f4e953',
    'id' : '8.3.7.2.1',
    'code' : '8.3.7.2.1',
    'abbr' : '8.3.7.2.1',
    'name' : { 'en': 'Worse' },
    'description' : 'Use this domain for words describing something that is worse than something else.',
    'value' : '8.3.7.2.1 Worse'
  },
  {
    'guid' : 'c7ccc5bb-181d-420f-8665-64793fefb37b',
    'id' : '8.3.7.3',
    'code' : '8.3.7.3',
    'abbr' : '8.3.7.3',
    'name' : { 'en': 'Perfect' },
    'description' : 'Use this domain for words describing something that is perfect.',
    'value' : '8.3.7.3 Perfect'
  },
  {
    'guid' : 'b59f6fc4-629d-4e62-8673-cf62f8ad8197',
    'id' : '8.3.7.4',
    'code' : '8.3.7.4',
    'abbr' : '8.3.7.4',
    'name' : { 'en': 'Mediocre' },
    'description' : 'Use this domain for words describing something that is mediocre.',
    'value' : '8.3.7.4 Mediocre'
  },
  {
    'guid' : '11665f1d-aca9-4699-afb2-bcdea69c6645',
    'id' : '8.3.7.5',
    'code' : '8.3.7.5',
    'abbr' : '8.3.7.5',
    'name' : { 'en': 'Important' },
    'description' : 'Use this domain for words describing something that is important.',
    'value' : '8.3.7.5 Important'
  },
  {
    'guid' : '8938e132-6534-4428-9b03-cb1f459b7cbe',
    'id' : '8.3.7.5.1',
    'code' : '8.3.7.5.1',
    'abbr' : '8.3.7.5.1',
    'name' : { 'en': 'Basic' },
    'description' : 'Use this domain for words describing something that is basic.',
    'value' : '8.3.7.5.1 Basic'
  },
  {
    'guid' : 'c9b5f83e-529d-45af-949f-4cc6b0591b66',
    'id' : '8.3.7.6',
    'code' : '8.3.7.6',
    'abbr' : '8.3.7.6',
    'name' : { 'en': 'Improve' },
    'description' : 'Use this domain for words related to improving something--to make something better.',
    'value' : '8.3.7.6 Improve'
  },
  {
    'guid' : 'f29dccae-1654-4eb7-8aae-04f7df4fe90c',
    'id' : '8.3.7.7',
    'code' : '8.3.7.7',
    'abbr' : '8.3.7.7',
    'name' : { 'en': 'Right, proper' },
    'description' : 'Use this domain for words describing something, such as a tool or way of doing something, that is proper for a particular time, place, purpose, or job. The words in this domain involve a comparison between two things, an item and a setting. An evaluation is made as to how good the item is in the setting. The words may be used only of certain types of items, certain types of settings, or certain types of usefulness.',
    'value' : '8.3.7.7 Right, proper'
  },
  {
    'guid' : '21f21658-a69a-491c-a37b-156a8f4ad3fb',
    'id' : '8.3.7.7.1',
    'code' : '8.3.7.7.1',
    'abbr' : '8.3.7.7.1',
    'name' : { 'en': 'Wrong, unsuitable' },
    'description' : 'Use this domain for words describing something, such as a tool or way of doing something, that is unsuitable for a particular time, place, purpose, or job.',
    'value' : '8.3.7.7.1 Wrong, unsuitable'
  },
  {
    'guid' : 'c7dbd50e-0ff5-42af-a7a9-9eaf03671c49',
    'id' : '8.3.7.7.2',
    'code' : '8.3.7.7.2',
    'abbr' : '8.3.7.7.2',
    'name' : { 'en': 'Convenient' },
    'description' : 'Use this domain for words describing something that is convenient--a good time to do something.',
    'value' : '8.3.7.7.2 Convenient'
  },
  {
    'guid' : '2b2bedd5-3f9c-4c18-a256-aa65ee19f15c',
    'id' : '8.3.7.7.3',
    'code' : '8.3.7.7.3',
    'abbr' : '8.3.7.7.3',
    'name' : { 'en': 'Compatible' },
    'description' : 'Use this domain for words related to being compatible--words that describe two things or people that can be together or work together without problems or conflict.',
    'value' : '8.3.7.7.3 Compatible'
  },
  {
    'guid' : '76a06e45-99f7-446f-a2e8-e23edf6064bd',
    'id' : '8.3.7.8',
    'code' : '8.3.7.8',
    'abbr' : '8.3.7.8',
    'name' : { 'en': 'Decay' },
    'description' : 'Use this domain for words related to something decaying--when a living thing dies and becomes bad, or when a part of a living thing becomes bad.',
    'value' : '8.3.7.8 Decay'
  },
  {
    'guid' : '94f50cb8-9a59-42cc-9891-247cc3de7428',
    'id' : '8.3.7.8.1',
    'code' : '8.3.7.8.1',
    'abbr' : '8.3.7.8.1',
    'name' : { 'en': 'Rust' },
    'description' : 'Use this domain for words related to metal rusting--when metal becomes bad.',
    'value' : '8.3.7.8.1 Rust'
  },
  {
    'guid' : 'de7b8df5-83a7-4456-a63a-1075ff17dbaf',
    'id' : '8.3.7.8.2',
    'code' : '8.3.7.8.2',
    'abbr' : '8.3.7.8.2',
    'name' : { 'en': 'Blemish' },
    'description' : 'Use this domain for words related to a blemish--something small and bad on the skin of a person or the surface of something, but not something serious, especially something wrong that does not affect how something works.',
    'value' : '8.3.7.8.2 Blemish'
  },
  {
    'guid' : '03352940-c220-4a32-a9a5-fc08d1d0dc71',
    'id' : '8.3.7.8.3',
    'code' : '8.3.7.8.3',
    'abbr' : '8.3.7.8.3',
    'name' : { 'en': 'Garbage' },
    'description' : 'Use this domain for words related to garbage--something that is no longer wanted.',
    'value' : '8.3.7.8.3 Garbage'
  },
  {
    'guid' : '98f9ceff-e8a2-4e24-abc4-561b80bb5889',
    'id' : '8.3.7.8.4',
    'code' : '8.3.7.8.4',
    'abbr' : '8.3.7.8.4',
    'name' : { 'en': 'Preserve' },
    'description' : 'Use this domain for words related to preserving the condition of something from decay.',
    'value' : '8.3.7.8.4 Preserve'
  },
  {
    'guid' : 'cd4300c9-265e-4457-8e33-4e0c9a4d4ba8',
    'id' : '8.3.7.9',
    'code' : '8.3.7.9',
    'abbr' : '8.3.7.9',
    'name' : { 'en': 'Value' },
    'description' : 'Use this domain for words related to the value of something.',
    'value' : '8.3.7.9 Value'
  },
  {
    'guid' : '130e2cbb-7e51-4f6f-a1cf-7a053a44c9b7',
    'id' : '8.3.8',
    'code' : '8.3.8',
    'abbr' : '8.3.8',
    'name' : { 'en': 'Decorated' },
    'description' : 'Use this domain for words describing something that is decorated.',
    'value' : '8.3.8 Decorated'
  },
  {
    'guid' : '91e95825-677a-4221-9e55-78a73bbac6ee',
    'id' : '8.3.8.1',
    'code' : '8.3.8.1',
    'abbr' : '8.3.8.1',
    'name' : { 'en': 'Simple, plain' },
    'description' : 'Use this domain for words describing something that is simple or plain.',
    'value' : '8.3.8.1 Simple, plain'
  },
  {
    'guid' : '312ce7a7-8c7c-416d-bf93-73376f1f16d8',
    'id' : '8.3.8.2',
    'code' : '8.3.8.2',
    'abbr' : '8.3.8.2',
    'name' : { 'en': 'Glory' },
    'description' : 'Use this domain for words describing the appearance of something that has pleasing aspects or inspires awe and wonder in the viewer. For instance, the palace of a king, the home of a rich man, or a temple may be elaborately decorated and be described as glorious or magnificent. Or something in nature such as a sunset or flower may inspire awe and wonder.',
    'value' : '8.3.8.2 Glory'
  },
  {
    'guid' : '21167445-f1b1-49b4-b147-bc792616c432',
    'id' : '8.4',
    'code' : '8.4',
    'abbr' : '8.4',
    'name' : { 'en': 'Time' },
    'description' : 'Use this domain for general words related to time, and for words indicating the temporal location of an event.',
    'value' : '8.4 Time'
  },
  {
    'guid' : '14ad95ad-50fc-450f-b44d-4273df0b1e8b',
    'id' : '8.4.1',
    'code' : '8.4.1',
    'abbr' : '8.4.1',
    'name' : { 'en': 'Period of time' },
    'description' : 'Use this domain for words referring to a period of time.',
    'value' : '8.4.1 Period of time'
  },
  {
    'guid' : 'f8c6a6a9-49f0-408a-9237-a66e852da7d3',
    'id' : '8.4.1.1',
    'code' : '8.4.1.1',
    'abbr' : '8.4.1.1',
    'name' : { 'en': 'Calendar' },
    'description' : 'Use this domain for words related to the calendar.',
    'value' : '8.4.1.1 Calendar'
  },
  {
    'guid' : 'afe7cc92-e0ad-40d9-be56-aa12d2693a3f',
    'id' : '8.4.1.2',
    'code' : '8.4.1.2',
    'abbr' : '8.4.1.2',
    'name' : { 'en': 'Day' },
    'description' : 'Use this domain for words referring to a day.',
    'value' : '8.4.1.2 Day'
  },
  {
    'guid' : '1088cc2f-83ae-4911-8018-401a745dcfd5',
    'id' : '8.4.1.2.1',
    'code' : '8.4.1.2.1',
    'abbr' : '8.4.1.2.1',
    'name' : { 'en': 'Night' },
    'description' : 'Use this domain for words referring to night.',
    'value' : '8.4.1.2.1 Night'
  },
  {
    'guid' : '3a0dc521-f028-4c17-945c-b121e2d3dc0b',
    'id' : '8.4.1.2.2',
    'code' : '8.4.1.2.2',
    'abbr' : '8.4.1.2.2',
    'name' : { 'en': 'Yesterday, today, tomorrow' },
    'description' : 'Use this domain for words referring to days relative to each other.',
    'value' : '8.4.1.2.2 Yesterday, today, tomorrow'
  },
  {
    'guid' : '0cc62b4a-d5ff-4f45-83d1-e2b46e5d159a',
    'id' : '8.4.1.2.3',
    'code' : '8.4.1.2.3',
    'abbr' : '8.4.1.2.3',
    'name' : { 'en': 'Time of the day' },
    'description' : 'Use this domain for words referring to a time of the day.',
    'value' : '8.4.1.2.3 Time of the day'
  },
  {
    'guid' : 'b21ace5f-9307-4bdc-b103-9fdf14a5655e',
    'id' : '8.4.1.3',
    'code' : '8.4.1.3',
    'abbr' : '8.4.1.3',
    'name' : { 'en': 'Week' },
    'description' : 'Use this domain for words related to a week.',
    'value' : '8.4.1.3 Week'
  },
  {
    'guid' : 'de544ebd-9f94-4831-8887-944c3bbbc254',
    'id' : '8.4.1.3.1',
    'code' : '8.4.1.3.1',
    'abbr' : '8.4.1.3.1',
    'name' : { 'en': 'Days of the week' },
    'description' : 'Use this domain for words referring to the days of the week.',
    'value' : '8.4.1.3.1 Days of the week'
  },
  {
    'guid' : '4e23b037-0547-4650-89c3-2b259b637fb6',
    'id' : '8.4.1.4',
    'code' : '8.4.1.4',
    'abbr' : '8.4.1.4',
    'name' : { 'en': 'Month' },
    'description' : 'Use this domain for words referring to a month.',
    'value' : '8.4.1.4 Month'
  },
  {
    'guid' : '46dbda42-fe21-4e52-8eeb-4263ded7031b',
    'id' : '8.4.1.4.1',
    'code' : '8.4.1.4.1',
    'abbr' : '8.4.1.4.1',
    'name' : { 'en': 'Months of the year' },
    'description' : 'Use this domain for words referring to the months of the year.',
    'value' : '8.4.1.4.1 Months of the year'
  },
  {
    'guid' : '0622d3f7-1ab2-482b-9f9c-9c101cd35182',
    'id' : '8.4.1.5',
    'code' : '8.4.1.5',
    'abbr' : '8.4.1.5',
    'name' : { 'en': 'Season' },
    'description' : 'Use this domain for words referring to seasons of the year that are related to the time of year, the weather, or times of cultivation.',
    'value' : '8.4.1.5 Season'
  },
  {
    'guid' : 'fcf16495-5226-4192-afdb-e748192efc3a',
    'id' : '8.4.1.6',
    'code' : '8.4.1.6',
    'abbr' : '8.4.1.6',
    'name' : { 'en': 'Year' },
    'description' : 'Use this domain for words referring to a year.',
    'value' : '8.4.1.6 Year'
  },
  {
    'guid' : '1689ac96-1159-4575-bf5f-d16345f9496c',
    'id' : '8.4.1.7',
    'code' : '8.4.1.7',
    'abbr' : '8.4.1.7',
    'name' : { 'en': 'Era' },
    'description' : 'Use this domain for words referring to an era--a very long period of time.',
    'value' : '8.4.1.7 Era'
  },
  {
    'guid' : '659ccabf-f978-4852-a1a6-c225f1d76b97',
    'id' : '8.4.1.8',
    'code' : '8.4.1.8',
    'abbr' : '8.4.1.8',
    'name' : { 'en': 'Special days' },
    'description' : 'Use this domain for words referring to a special day.',
    'value' : '8.4.1.8 Special days'
  },
  {
    'guid' : 'c2b720f5-1123-446e-9f60-088a3272b889',
    'id' : '8.4.2',
    'code' : '8.4.2',
    'abbr' : '8.4.2',
    'name' : { 'en': 'Take time' },
    'description' : 'Use this domain for words referring to taking time to do something.',
    'value' : '8.4.2 Take time'
  },
  {
    'guid' : '5bb495a5-ab5b-4409-8cd1-e48b56401fad',
    'id' : '8.4.2.1',
    'code' : '8.4.2.1',
    'abbr' : '8.4.2.1',
    'name' : { 'en': 'A short time' },
    'description' : 'Use this domain for words referring to a short time.',
    'value' : '8.4.2.1 A short time'
  },
  {
    'guid' : '7ea946fb-6469-4f21-a5a4-7963878e6fe2',
    'id' : '8.4.2.2',
    'code' : '8.4.2.2',
    'abbr' : '8.4.2.2',
    'name' : { 'en': 'A long time' },
    'description' : 'Use this domain for words referring to a long time.',
    'value' : '8.4.2.2 A long time'
  },
  {
    'guid' : 'c8185ca6-567a-40ef-939f-ffefdd9a4770',
    'id' : '8.4.2.3',
    'code' : '8.4.2.3',
    'abbr' : '8.4.2.3',
    'name' : { 'en': 'Forever' },
    'description' : 'Use this domain for words referring to something happening forever.',
    'value' : '8.4.2.3 Forever'
  },
  {
    'guid' : 'f3a26e0a-727f-43ab-9310-88b8cec8f6d7',
    'id' : '8.4.2.4',
    'code' : '8.4.2.4',
    'abbr' : '8.4.2.4',
    'name' : { 'en': 'Temporary' },
    'description' : 'Use this domain for words referring to something being temporary.',
    'value' : '8.4.2.4 Temporary'
  },
  {
    'guid' : '532245e7-8f46-4394-9045-240475ee62e8',
    'id' : '8.4.3',
    'code' : '8.4.3',
    'abbr' : '8.4.3',
    'name' : { 'en': 'Indefinite time' },
    'description' : 'Use this domain for words referring to an indefinite time.',
    'value' : '8.4.3 Indefinite time'
  },
  {
    'guid' : '06cb2024-5f7b-467c-b32c-ef4c56030ac0',
    'id' : '8.4.4',
    'code' : '8.4.4',
    'abbr' : '8.4.4',
    'name' : { 'en': 'Telling time' },
    'description' : 'Use this domain for words related to telling time.',
    'value' : '8.4.4 Telling time'
  },
  {
    'guid' : '12d752d5-53a9-46f6-9e81-3153401cc760',
    'id' : '8.4.4.1',
    'code' : '8.4.4.1',
    'abbr' : '8.4.4.1',
    'name' : { 'en': 'Plan a time' },
    'description' : 'Use this domain for words referring to planning the time of an event.',
    'value' : '8.4.4.1 Plan a time'
  },
  {
    'guid' : 'c4fdb9ce-93cc-405b-b673-4058821bf794',
    'id' : '8.4.4.2',
    'code' : '8.4.4.2',
    'abbr' : '8.4.4.2',
    'name' : { 'en': 'Clock, watch' },
    'description' : 'Use this domain for machines that indicate what time it is.',
    'value' : '8.4.4.2 Clock, watch'
  },
  {
    'guid' : '1f4efae7-1029-4b66-80ee-802459a7baf5',
    'id' : '8.4.5',
    'code' : '8.4.5',
    'abbr' : '8.4.5',
    'name' : { 'en': 'Relative time' },
    'description' : 'Use the domains in this section for words that relate one time to another. Use this domain for words that indicate a temporal relation between situations.',
    'value' : '8.4.5 Relative time'
  },
  {
    'guid' : '00269021-e1c4-474d-9dba-341d296bdac7',
    'id' : '8.4.5.1',
    'code' : '8.4.5.1',
    'abbr' : '8.4.5.1',
    'name' : { 'en': 'Order, sequence' },
    'description' : 'Use this domain for words referring to temporal order or sequence--the order in which a group of events happen. Things and people may also be in order based on the order in which something happened or should happen to them.',
    'value' : '8.4.5.1 Order, sequence'
  },
  {
    'guid' : '00041516-72d1-4e56-9ed8-fe235a9b1a68',
    'id' : '8.4.5.1.1',
    'code' : '8.4.5.1.1',
    'abbr' : '8.4.5.1.1',
    'name' : { 'en': 'Series' },
    'description' : 'Use this domain for words related to a series--several things that happen one after another.',
    'value' : '8.4.5.1.1 Series'
  },
  {
    'guid' : 'f352a437-58f2-4920-aec3-eda8041f7447',
    'id' : '8.4.5.1.2',
    'code' : '8.4.5.1.2',
    'abbr' : '8.4.5.1.2',
    'name' : { 'en': 'First' },
    'description' : 'Use this domain for words referring to something happening first--to be before all other things in order or time.',
    'value' : '8.4.5.1.2 First'
  },
  {
    'guid' : 'a345d090-14e2-4897-9186-debcb05ab27c',
    'id' : '8.4.5.1.3',
    'code' : '8.4.5.1.3',
    'abbr' : '8.4.5.1.3',
    'name' : { 'en': 'Next' },
    'description' : 'Use this domain for words referring to something happening next.',
    'value' : '8.4.5.1.3 Next'
  },
  {
    'guid' : '49aa89f2-2022-4213-845e-dbbb4b53476c',
    'id' : '8.4.5.1.4',
    'code' : '8.4.5.1.4',
    'abbr' : '8.4.5.1.4',
    'name' : { 'en': 'Last' },
    'description' : 'Use this domain for words referring to something happening last--to happen after all other things in a sequence, or to be the last person or thing in a sequence.',
    'value' : '8.4.5.1.4 Last'
  },
  {
    'guid' : '6712385a-6740-4f28-8bbe-8615ea17116b',
    'id' : '8.4.5.1.5',
    'code' : '8.4.5.1.5',
    'abbr' : '8.4.5.1.5',
    'name' : { 'en': 'Regular' },
    'description' : 'Use this domain for words referring to something that happens regularly.',
    'value' : '8.4.5.1.5 Regular'
  },
  {
    'guid' : '95a8d932-9554-439f-afb5-ab158f2eed96',
    'id' : '8.4.5.1.6',
    'code' : '8.4.5.1.6',
    'abbr' : '8.4.5.1.6',
    'name' : { 'en': 'Alternate' },
    'description' : 'Use this domain for words related to alternating--when several things happen one after another in a repeated pattern.',
    'value' : '8.4.5.1.6 Alternate'
  },
  {
    'guid' : 'd80360f9-7319-40a7-a2bc-fd8718711ba4',
    'id' : '8.4.5.2',
    'code' : '8.4.5.2',
    'abbr' : '8.4.5.2',
    'name' : { 'en': 'Before' },
    'description' : 'Use this domain for words referring to one event happening before another.',
    'value' : '8.4.5.2 Before'
  },
  {
    'guid' : 'bdaecf2f-d0fa-49f7-891e-bcb0a31ae630',
    'id' : '8.4.5.2.1',
    'code' : '8.4.5.2.1',
    'abbr' : '8.4.5.2.1',
    'name' : { 'en': 'After' },
    'description' : 'Use this domain for words referring to one event happening after another.',
    'value' : '8.4.5.2.1 After'
  },
  {
    'guid' : '9f8b8c01-f790-469f-bc37-dece6227e276',
    'id' : '8.4.5.2.2',
    'code' : '8.4.5.2.2',
    'abbr' : '8.4.5.2.2',
    'name' : { 'en': 'At the same time' },
    'description' : 'Use this domain for words referring to two things happening at the same time.',
    'value' : '8.4.5.2.2 At the same time'
  },
  {
    'guid' : '69d039e6-f669-4d54-8b67-89b19ff0a19c',
    'id' : '8.4.5.2.3',
    'code' : '8.4.5.2.3',
    'abbr' : '8.4.5.2.3',
    'name' : { 'en': 'During' },
    'description' : 'Use this domain for words indicating that something happened during some time period, or that something happened while something else was happening.',
    'value' : '8.4.5.2.3 During'
  },
  {
    'guid' : '6bf8ce57-1970-4efb-a7d7-b0bf8be6fb6b',
    'id' : '8.4.5.3',
    'code' : '8.4.5.3',
    'abbr' : '8.4.5.3',
    'name' : { 'en': 'Right time' },
    'description' : 'Use this domain for words referring to the right time to do something.',
    'value' : '8.4.5.3 Right time'
  },
  {
    'guid' : 'eb662979-604c-455e-a2c6-a84b03a2ee3a',
    'id' : '8.4.5.3.1',
    'code' : '8.4.5.3.1',
    'abbr' : '8.4.5.3.1',
    'name' : { 'en': 'Early' },
    'description' : 'Use this domain for words that indicate that something happens early--before the expected time, before the usual time, or before the time that was agreed on. Some words may include the idea that it is good that the event happened early. Other words may include the idea that it is bad that the event happened early.',
    'value' : '8.4.5.3.1 Early'
  },
  {
    'guid' : 'b7b0819b-eceb-4f16-ae6d-2298c4df1e6f',
    'id' : '8.4.5.3.2',
    'code' : '8.4.5.3.2',
    'abbr' : '8.4.5.3.2',
    'name' : { 'en': 'On time' },
    'description' : 'Use this domain for words describing something happening on time--at the expected time, at the usual time, or at the time that was agreed on.',
    'value' : '8.4.5.3.2 On time'
  },
  {
    'guid' : '6fb1d03c-b0fe-49b7-a473-168f12a54a36',
    'id' : '8.4.5.3.3',
    'code' : '8.4.5.3.3',
    'abbr' : '8.4.5.3.3',
    'name' : { 'en': 'Late' },
    'description' : 'Use this domain for words describing something happening late--after the expected time, after the usual time, or after the time that was agreed on.',
    'value' : '8.4.5.3.3 Late'
  },
  {
    'guid' : '4526b41d-6f3c-494f-93a2-ea3e9705269d',
    'id' : '8.4.5.3.4',
    'code' : '8.4.5.3.4',
    'abbr' : '8.4.5.3.4',
    'name' : { 'en': 'Delay' },
    'description' : 'Use this domain for words referring to something delaying someone or something--to cause something to happen at a later time, cause someone to do something at a later time, or cause someone or something to be late.',
    'value' : '8.4.5.3.4 Delay'
  },
  {
    'guid' : 'daefd275-98e3-4534-a991-c7d396b54c69',
    'id' : '8.4.5.3.5',
    'code' : '8.4.5.3.5',
    'abbr' : '8.4.5.3.5',
    'name' : { 'en': 'Postpone' },
    'description' : 'Use this domain for words referring to postponing something--to decide to do something later.',
    'value' : '8.4.5.3.5 Postpone'
  },
  {
    'guid' : '18a6684f-d324-45ee-855c-44d473916b14',
    'id' : '8.4.6',
    'code' : '8.4.6',
    'abbr' : '8.4.6',
    'name' : { 'en': 'Aspectual time' },
    'description' : 'Use this domain for words referring to a time period that is part of a longer time period.',
    'value' : '8.4.6 Aspectual time'
  },
  {
    'guid' : '3f37bb6f-cd32-4430-aa35-700acabbee15',
    'id' : '8.4.6.1',
    'code' : '8.4.6.1',
    'abbr' : '8.4.6.1',
    'name' : { 'en': 'Start something' },
    'description' : 'Use this domain for words referring to starting something, or for something beginning to happen.',
    'value' : '8.4.6.1 Start something'
  },
  {
    'guid' : '5261497b-6beb-4db1-9de2-10b5f6f8ec69',
    'id' : '8.4.6.1.1',
    'code' : '8.4.6.1.1',
    'abbr' : '8.4.6.1.1',
    'name' : { 'en': 'Beginning' },
    'description' : 'Use this domain for words referring to something beginning to happen, to beginning to do something, to cause something to start happening, or to cause people to start doing something.',
    'value' : '8.4.6.1.1 Beginning'
  },
  {
    'guid' : '2c42f822-2079-440c-b3b7-7725b6a8db8b',
    'id' : '8.4.6.1.2',
    'code' : '8.4.6.1.2',
    'abbr' : '8.4.6.1.2',
    'name' : { 'en': 'Stop something' },
    'description' : 'Use this domain for words related to the end of an action or situation.',
    'value' : '8.4.6.1.2 Stop something'
  },
  {
    'guid' : '86615235-8cdd-413d-b722-11bc5a4653d6',
    'id' : '8.4.6.1.3',
    'code' : '8.4.6.1.3',
    'abbr' : '8.4.6.1.3',
    'name' : { 'en': 'End' },
    'description' : 'Use this domain for words referring to the end of an action or situation.',
    'value' : '8.4.6.1.3 End'
  },
  {
    'guid' : '590570c5-3267-4966-b0db-2af7a5105c83',
    'id' : '8.4.6.1.4',
    'code' : '8.4.6.1.4',
    'abbr' : '8.4.6.1.4',
    'name' : { 'en': 'Until' },
    'description' : 'Use this domain for words that indicate that something will continue to happen until a particular time or until something else happens, and then it will stop.',
    'value' : '8.4.6.1.4 Until'
  },
  {
    'guid' : '3edb307f-be46-40b6-a6a4-ae075b40258c',
    'id' : '8.4.6.1.5',
    'code' : '8.4.6.1.5',
    'abbr' : '8.4.6.1.5',
    'name' : { 'en': 'Since, from' },
    'description' : 'Use this domain for words that indicate that something will start to happen at some time and continue for some time.',
    'value' : '8.4.6.1.5 Since, from'
  },
  {
    'guid' : 'bbc5b3a2-4c6e-4d07-849b-4d616615a794',
    'id' : '8.4.6.2',
    'code' : '8.4.6.2',
    'abbr' : '8.4.6.2',
    'name' : { 'en': 'Past' },
    'description' : 'Use this domain for words referring to the past or to a time in the past.',
    'value' : '8.4.6.2 Past'
  },
  {
    'guid' : '0656cd5e-641f-46f3-bcad-6f643727a344',
    'id' : '8.4.6.2.1',
    'code' : '8.4.6.2.1',
    'abbr' : '8.4.6.2.1',
    'name' : { 'en': 'Recently' },
    'description' : 'Use this domain for words indicating that something happened recently--a short time before now.',
    'value' : '8.4.6.2.1 Recently'
  },
  {
    'guid' : '7b816f6a-4b46-403d-a1a2-2914ee070568',
    'id' : '8.4.6.3',
    'code' : '8.4.6.3',
    'abbr' : '8.4.6.3',
    'name' : { 'en': 'Present' },
    'description' : 'Use this domain for words referring to the present time.',
    'value' : '8.4.6.3 Present'
  },
  {
    'guid' : '3fbe3ea6-3ad3-430f-ab67-2d9c9f852c61',
    'id' : '8.4.6.3.1',
    'code' : '8.4.6.3.1',
    'abbr' : '8.4.6.3.1',
    'name' : { 'en': 'Now' },
    'description' : 'Use this domain for words referring to now.',
    'value' : '8.4.6.3.1 Now'
  },
  {
    'guid' : '50c1a392-2928-407a-8306-3c70141e375e',
    'id' : '8.4.6.4',
    'code' : '8.4.6.4',
    'abbr' : '8.4.6.4',
    'name' : { 'en': 'Future' },
    'description' : 'Use this domain for words referring to the future.',
    'value' : '8.4.6.4 Future'
  },
  {
    'guid' : 'd27a5602-ece1-452e-9ed6-7261082dc8b8',
    'id' : '8.4.6.4.1',
    'code' : '8.4.6.4.1',
    'abbr' : '8.4.6.4.1',
    'name' : { 'en': 'Soon' },
    'description' : 'Use this domain for words referring to something happening soon.',
    'value' : '8.4.6.4.1 Soon'
  },
  {
    'guid' : '99e38da1-91ad-4dff-a68f-972607936e50',
    'id' : '8.4.6.4.2',
    'code' : '8.4.6.4.2',
    'abbr' : '8.4.6.4.2',
    'name' : { 'en': 'Not yet' },
    'description' : 'Use this domain for words referring to something not happening yet.',
    'value' : '8.4.6.4.2 Not yet'
  },
  {
    'guid' : '73ea23c2-db45-49fa-a72b-0fc6eff4ce30',
    'id' : '8.4.6.4.3',
    'code' : '8.4.6.4.3',
    'abbr' : '8.4.6.4.3',
    'name' : { 'en': 'Eventually' },
    'description' : 'Use this domain for words referring to something happening eventually.',
    'value' : '8.4.6.4.3 Eventually'
  },
  {
    'guid' : '04543543-4c3d-4d71-aa87-53191ef3b7b0',
    'id' : '8.4.6.4.4',
    'code' : '8.4.6.4.4',
    'abbr' : '8.4.6.4.4',
    'name' : { 'en': 'Immediately' },
    'description' : 'Use this domain for words referring to something happening immediately--without any time passing before it happens.',
    'value' : '8.4.6.4.4 Immediately'
  },
  {
    'guid' : '615674ac-8158-4089-ae70-b55472fd279b',
    'id' : '8.4.6.5',
    'code' : '8.4.6.5',
    'abbr' : '8.4.6.5',
    'name' : { 'en': 'Age' },
    'description' : 'Use this domain for words referring to the age of something.',
    'value' : '8.4.6.5 Age'
  },
  {
    'guid' : '0d38c343-9c51-47fe-a367-ffadfc92c507',
    'id' : '8.4.6.5.1',
    'code' : '8.4.6.5.1',
    'abbr' : '8.4.6.5.1',
    'name' : { 'en': 'Young' },
    'description' : 'Use this domain for words describing something young--a word describing a living thing that has only existed for a short time.',
    'value' : '8.4.6.5.1 Young'
  },
  {
    'guid' : 'cc6f100a-5220-4f53-801c-b1fdcc619608',
    'id' : '8.4.6.5.2',
    'code' : '8.4.6.5.2',
    'abbr' : '8.4.6.5.2',
    'name' : { 'en': 'Old, not young' },
    'description' : 'Use this domain for words describing something old--a word describing a living thing that has existed for a long time.',
    'value' : '8.4.6.5.2 Old, not young'
  },
  {
    'guid' : 'efef45bd-26be-46f8-b85b-424be55bcdac',
    'id' : '8.4.6.5.3',
    'code' : '8.4.6.5.3',
    'abbr' : '8.4.6.5.3',
    'name' : { 'en': 'New' },
    'description' : 'Use this domain for words describing something new--a word describing something that has only existed for a short time.',
    'value' : '8.4.6.5.3 New'
  },
  {
    'guid' : '99861dcb-c6ca-4e50-a19a-efadbb10c2cf',
    'id' : '8.4.6.5.4',
    'code' : '8.4.6.5.4',
    'abbr' : '8.4.6.5.4',
    'name' : { 'en': 'Old, not new' },
    'description' : 'Use this domain for words describing something old--a word describing something that has existed for a long time.',
    'value' : '8.4.6.5.4 Old, not new'
  },
  {
    'guid' : '31e0fde8-b3ab-47ae-b791-54309e6ed0bd',
    'id' : '8.4.6.5.5',
    'code' : '8.4.6.5.5',
    'abbr' : '8.4.6.5.5',
    'name' : { 'en': 'Modern' },
    'description' : 'Use this domain for words describing something modern--a word that describes something like a machine, system, or country that uses the most recent equipment, ideas, and methods.',
    'value' : '8.4.6.5.5 Modern'
  },
  {
    'guid' : 'b015f460-faeb-4aa5-b453-9e5e9ae061fe',
    'id' : '8.4.6.5.6',
    'code' : '8.4.6.5.6',
    'abbr' : '8.4.6.5.6',
    'name' : { 'en': 'Old fashioned' },
    'description' : 'Use this domain for words describing something old fashioned--something that was done or used in the past, but not done or used now.',
    'value' : '8.4.6.5.6 Old fashioned'
  },
  {
    'guid' : '457231c8-4eb6-4460-aa45-3e9f2c4e8975',
    'id' : '8.4.6.6',
    'code' : '8.4.6.6',
    'abbr' : '8.4.6.6',
    'name' : { 'en': 'Once' },
    'description' : 'Use this domain for words referring to something happening once.',
    'value' : '8.4.6.6 Once'
  },
  {
    'guid' : '2351f52a-8822-46ad-99c4-7ef526e94a6f',
    'id' : '8.4.6.6.1',
    'code' : '8.4.6.6.1',
    'abbr' : '8.4.6.6.1',
    'name' : { 'en': 'Again' },
    'description' : 'Use this domain for words referring to something happening again or doing something again.',
    'value' : '8.4.6.6.1 Again'
  },
  {
    'guid' : '776de0c6-fdd7-46df-b33f-b1e4af6ee099',
    'id' : '8.4.6.6.2',
    'code' : '8.4.6.6.2',
    'abbr' : '8.4.6.6.2',
    'name' : { 'en': 'Sometimes' },
    'description' : 'Use this domain for words referring to something happening sometimes.',
    'value' : '8.4.6.6.2 Sometimes'
  },
  {
    'guid' : '12f12bf3-f232-4477-bf39-d91b7f55c2c3',
    'id' : '8.4.6.6.3',
    'code' : '8.4.6.6.3',
    'abbr' : '8.4.6.6.3',
    'name' : { 'en': 'Often' },
    'description' : 'Use this domain for words referring to something happening often--happening or done many times.',
    'value' : '8.4.6.6.3 Often'
  },
  {
    'guid' : '03d65d0c-aafb-40c0-9cd2-3e5ced66ad03',
    'id' : '8.4.6.6.4',
    'code' : '8.4.6.6.4',
    'abbr' : '8.4.6.6.4',
    'name' : { 'en': 'All the time' },
    'description' : 'Use this domain for words referring to something happening all the time.',
    'value' : '8.4.6.6.4 All the time'
  },
  {
    'guid' : '1c8da3aa-3c74-4188-8949-5ab82fc1f99c',
    'id' : '8.4.6.6.5',
    'code' : '8.4.6.6.5',
    'abbr' : '8.4.6.6.5',
    'name' : { 'en': 'Every time' },
    'description' : 'Use this domain for words referring to something happening every time something else happens.',
    'value' : '8.4.6.6.5 Every time'
  },
  {
    'guid' : '4e2adaed-145e-45fc-8448-81c0bd47c414',
    'id' : '8.4.6.6.6',
    'code' : '8.4.6.6.6',
    'abbr' : '8.4.6.6.6',
    'name' : { 'en': 'Never' },
    'description' : 'Use this domain for words that indicate that something that never happens, or that something has not once happened.',
    'value' : '8.4.6.6.6 Never'
  },
  {
    'guid' : '780fbf89-f2ba-404c-b288-f6ca637bbc90',
    'id' : '8.4.7',
    'code' : '8.4.7',
    'abbr' : '8.4.7',
    'name' : { 'en': 'Continue, persevere' },
    'description' : 'Use this domain for words referring to continuing to do something.',
    'value' : '8.4.7 Continue, persevere'
  },
  {
    'guid' : '0d7409ab-fc1f-4680-b040-d91d7004084f',
    'id' : '8.4.7.1',
    'code' : '8.4.7.1',
    'abbr' : '8.4.7.1',
    'name' : { 'en': 'Interrupt' },
    'description' : 'Use this domain for words referring to interrupting someone--speaking when someone is speaking, or doing something to stop someone from doing what they are doing.',
    'value' : '8.4.7.1 Interrupt'
  },
  {
    'guid' : '08244b88-bfba-487a-96bc-ca3771d1fa7c',
    'id' : '8.4.7.2',
    'code' : '8.4.7.2',
    'abbr' : '8.4.7.2',
    'name' : { 'en': 'Start again' },
    'description' : 'Use this domain for words referring to starting to do something after stopping for some time.',
    'value' : '8.4.7.2 Start again'
  },
  {
    'guid' : '76d4c718-a84d-4b7b-9767-1c350c3bc124',
    'id' : '8.4.7.3',
    'code' : '8.4.7.3',
    'abbr' : '8.4.7.3',
    'name' : { 'en': 'Interval' },
    'description' : 'Use this domain for words referring to an interval between two events.',
    'value' : '8.4.7.3 Interval'
  },
  {
    'guid' : 'df149819-608f-46cd-ba0f-55f1d9d2e8ec',
    'id' : '8.4.8',
    'code' : '8.4.8',
    'abbr' : '8.4.8',
    'name' : { 'en': 'Speed' },
    'description' : 'Use this domain for words referring to the speed at which a person acts or the speed at which something happens.',
    'value' : '8.4.8 Speed'
  },
  {
    'guid' : '2fc69f71-e9f1-45f9-b88e-bdaf97457fc3',
    'id' : '8.4.8.1',
    'code' : '8.4.8.1',
    'abbr' : '8.4.8.1',
    'name' : { 'en': 'Quick' },
    'description' : 'Use this domain for words referring to doing something at a quick speed or something happening quickly.',
    'value' : '8.4.8.1 Quick'
  },
  {
    'guid' : 'af399519-5d7c-4100-9c79-8162cb4641cb',
    'id' : '8.4.8.2',
    'code' : '8.4.8.2',
    'abbr' : '8.4.8.2',
    'name' : { 'en': 'Slow' },
    'description' : 'Use this domain for words referring to doing something at a slow speed.',
    'value' : '8.4.8.2 Slow'
  },
  {
    'guid' : '4c31ac6a-3197-4762-9937-2fdea90784b7',
    'id' : '8.4.8.3',
    'code' : '8.4.8.3',
    'abbr' : '8.4.8.3',
    'name' : { 'en': 'Sudden' },
    'description' : 'Use this domain for words referring to a sudden event--something happens that I don"t expect.',
    'value' : '8.4.8.3 Sudden'
  },
  {
    'guid' : '2dca9338-85cb-4f58-b40d-d2d759e8edd6',
    'id' : '8.5',
    'code' : '8.5',
    'abbr' : '8.5',
    'name' : { 'en': 'Location' },
    'description' : 'Use this domain for words that refer to the place where something is located and for words indicating the location of something.',
    'value' : '8.5 Location'
  },
  {
    'guid' : 'f76c3803-1c7a-4181-9a87-64ae7231a67d',
    'id' : '8.5.1',
    'code' : '8.5.1',
    'abbr' : '8.5.1',
    'name' : { 'en': 'Here, there' },
    'description' : 'Use this domain for words that refer to a place in relation to the speaker or listener.',
    'value' : '8.5.1 Here, there'
  },
  {
    'guid' : '70f80041-af88-4521-9ebd-21d8f0b0d131',
    'id' : '8.5.1.1',
    'code' : '8.5.1.1',
    'abbr' : '8.5.1.1',
    'name' : { 'en': 'In front of' },
    'description' : 'Use this domain for words referring to being in front of you.',
    'value' : '8.5.1.1 In front of'
  },
  {
    'guid' : '775b53f1-bfcf-4270-a60d-b5affc9d6a99',
    'id' : '8.5.1.1.1',
    'code' : '8.5.1.1.1',
    'abbr' : '8.5.1.1.1',
    'name' : { 'en': 'Behind' },
    'description' : 'Use this domain for words indicating that something is behind you.',
    'value' : '8.5.1.1.1 Behind'
  },
  {
    'guid' : 'b8fc54d8-afd2-4ef8-b811-efb8aa7064db',
    'id' : '8.5.1.2',
    'code' : '8.5.1.2',
    'abbr' : '8.5.1.2',
    'name' : { 'en': 'Beside' },
    'description' : 'Use this domain for words that indicate that something is to the side of someone.',
    'value' : '8.5.1.2 Beside'
  },
  {
    'guid' : '6bc8e911-36f2-4d45-b237-2bdb6c03cc11',
    'id' : '8.5.1.2.1',
    'code' : '8.5.1.2.1',
    'abbr' : '8.5.1.2.1',
    'name' : { 'en': 'Around' },
    'description' : 'Use this domain for words indicating that something is around something else.',
    'value' : '8.5.1.2.1 Around'
  },
  {
    'guid' : 'c7c2d82e-d86c-4bf3-81a1-82772e87d709',
    'id' : '8.5.1.2.2',
    'code' : '8.5.1.2.2',
    'abbr' : '8.5.1.2.2',
    'name' : { 'en': 'Between' },
    'description' : 'Use this domain for words indicating that something is between two other things.',
    'value' : '8.5.1.2.2 Between'
  },
  {
    'guid' : '86287a4c-0d64-4f28-9a5c-17fb9df37ab6',
    'id' : '8.5.1.3',
    'code' : '8.5.1.3',
    'abbr' : '8.5.1.3',
    'name' : { 'en': 'On' },
    'description' : 'Use this domain for words indicating that something is on another thing.',
    'value' : '8.5.1.3 On'
  },
  {
    'guid' : 'c13ca251-6103-4475-85af-933311923f2c',
    'id' : '8.5.1.3.1',
    'code' : '8.5.1.3.1',
    'abbr' : '8.5.1.3.1',
    'name' : { 'en': 'Above' },
    'description' : 'Use this domain for words that express the idea that something is above another thing. The concept "above" is inherently relational, expressing the relative positions of two things.',
    'value' : '8.5.1.3.1 Above'
  },
  {
    'guid' : '0a1ad4c9-8bf3-448b-a27f-611813b305de',
    'id' : '8.5.1.3.2',
    'code' : '8.5.1.3.2',
    'abbr' : '8.5.1.3.2',
    'name' : { 'en': 'Under, below' },
    'description' : 'Use this domain for words that express the idea that something is under another thing. The concept "under" is inherently relational, expressing the relative positions of two things.',
    'value' : '8.5.1.3.2 Under, below'
  },
  {
    'guid' : '2265a4bd-379d-4a9d-80d5-2318e6c8c683',
    'id' : '8.5.1.4',
    'code' : '8.5.1.4',
    'abbr' : '8.5.1.4',
    'name' : { 'en': 'Inside' },
    'description' : 'Use this domain for words indicating that something is outside something else.',
    'value' : '8.5.1.4 Inside'
  },
  {
    'guid' : 'f4829d9d-a93f-4fc5-918c-6d4c501a6573',
    'id' : '8.5.1.4.1',
    'code' : '8.5.1.4.1',
    'abbr' : '8.5.1.4.1',
    'name' : { 'en': 'Out, outside' },
    'description' : 'Use this domain for words indicating that something is outside of another thing.',
    'value' : '8.5.1.4.1 Out, outside'
  },
  {
    'guid' : '8596f086-ee46-4245-8d45-2171a60e19e4',
    'id' : '8.5.1.5',
    'code' : '8.5.1.5',
    'abbr' : '8.5.1.5',
    'name' : { 'en': 'Touching, contact' },
    'description' : 'Use this domain for words indicating that two things are touching or in contact with each other.',
    'value' : '8.5.1.5 Touching, contact'
  },
  {
    'guid' : 'bcb1252c-7cb4-4fbc-b83f-e5f9c65b4afb',
    'id' : '8.5.1.5.1',
    'code' : '8.5.1.5.1',
    'abbr' : '8.5.1.5.1',
    'name' : { 'en': 'Next to' },
    'description' : 'Use this domain for words indicating that something is next to something else.',
    'value' : '8.5.1.5.1 Next to'
  },
  {
    'guid' : 'd2001c9c-3c37-4910-8b8a-adcffc6fbf26',
    'id' : '8.5.1.6',
    'code' : '8.5.1.6',
    'abbr' : '8.5.1.6',
    'name' : { 'en': 'Across' },
    'description' : 'Use this domain for words that refer to a place on another side of something from the reference point. "Across" involves three things--the object, the reference point, and something else in between the two.',
    'value' : '8.5.1.6 Across'
  },
  {
    'guid' : '2c143278-3ea0-49c6-9e50-e0bf7c8cf4e2',
    'id' : '8.5.1.7',
    'code' : '8.5.1.7',
    'abbr' : '8.5.1.7',
    'name' : { 'en': 'Indefinite location' },
    'description' : 'Use this domain for words indicating that something is at an indefinite location.',
    'value' : '8.5.1.7 Indefinite location'
  },
  {
    'guid' : '4f485a60-e3ba-42e6-9d59-185305c5d1f2',
    'id' : '8.5.2',
    'code' : '8.5.2',
    'abbr' : '8.5.2',
    'name' : { 'en': 'Direction' },
    'description' : 'Use this domain for general words referring to a direction.',
    'value' : '8.5.2 Direction'
  },
  {
    'guid' : '095c36bd-b74a-44f5-987b-85909e3f4c1d',
    'id' : '8.5.2.1',
    'code' : '8.5.2.1',
    'abbr' : '8.5.2.1',
    'name' : { 'en': 'Forward' },
    'description' : 'Use this domain for words indicating a forward direction.',
    'value' : '8.5.2.1 Forward'
  },
  {
    'guid' : '9ed66151-c144-4e5e-a3a2-f5d08b0c9bb8',
    'id' : '8.5.2.2',
    'code' : '8.5.2.2',
    'abbr' : '8.5.2.2',
    'name' : { 'en': 'Backward' },
    'description' : 'Use this domain for words indicating a backward direction.',
    'value' : '8.5.2.2 Backward'
  },
  {
    'guid' : '1a8322d7-cda9-41e5-a14b-f41274cb7157',
    'id' : '8.5.2.3',
    'code' : '8.5.2.3',
    'abbr' : '8.5.2.3',
    'name' : { 'en': 'Right, left' },
    'description' : 'Use this domain for words referring to right and left.',
    'value' : '8.5.2.3 Right, left'
  },
  {
    'guid' : 'e0ad6bb1-d422-408a-83f8-f1a7661ed225',
    'id' : '8.5.2.4',
    'code' : '8.5.2.4',
    'abbr' : '8.5.2.4',
    'name' : { 'en': 'Up' },
    'description' : 'Use this domain for words referring to the direction up.',
    'value' : '8.5.2.4 Up'
  },
  {
    'guid' : '84084e67-b321-4b6a-a436-29ead0bee586',
    'id' : '8.5.2.5',
    'code' : '8.5.2.5',
    'abbr' : '8.5.2.5',
    'name' : { 'en': 'Down' },
    'description' : 'Use this domain for words referring to the direction down.',
    'value' : '8.5.2.5 Down'
  },
  {
    'guid' : '3420b36a-a033-4af9-a8c4-53f8221ee56e',
    'id' : '8.5.2.6',
    'code' : '8.5.2.6',
    'abbr' : '8.5.2.6',
    'name' : { 'en': 'Away from' },
    'description' : 'Use this domain for words indicating a direction away from something.',
    'value' : '8.5.2.6 Away from'
  },
  {
    'guid' : '21a284ab-b9a3-42c8-8fb9-96aff1e1fe8f',
    'id' : '8.5.2.7',
    'code' : '8.5.2.7',
    'abbr' : '8.5.2.7',
    'name' : { 'en': 'Towards' },
    'description' : 'Use this domain for words indicating a direction toward something.',
    'value' : '8.5.2.7 Towards'
  },
  {
    'guid' : '6b1eeebd-2433-4f39-9e67-7ac4dd0fe20a',
    'id' : '8.5.2.8',
    'code' : '8.5.2.8',
    'abbr' : '8.5.2.8',
    'name' : { 'en': 'North, south, east, west' },
    'description' : 'Use this domain for words referring to the directions of the compass.',
    'value' : '8.5.2.8 North, south, east, west'
  },
  {
    'guid' : '80bcbc99-3c85-46d6-b15c-895367231747',
    'id' : '8.5.3',
    'code' : '8.5.3',
    'abbr' : '8.5.3',
    'name' : { 'en': 'Be at a place' },
    'description' : 'Use this domain for words related to being at a place.',
    'value' : '8.5.3 Be at a place'
  },
  {
    'guid' : '8affe94d-a396-4404-a0d7-c65046e617e6',
    'id' : '8.5.3.1',
    'code' : '8.5.3.1',
    'abbr' : '8.5.3.1',
    'name' : { 'en': 'Absent' },
    'description' : 'Use this domain for words related to being absent--to not be in a particular place, or not be in the correct or expected place.',
    'value' : '8.5.3.1 Absent'
  },
  {
    'guid' : 'a4b03891-c2df-4d72-bbdc-13bc8d4eceba',
    'id' : '8.5.4',
    'code' : '8.5.4',
    'abbr' : '8.5.4',
    'name' : { 'en': 'Area' },
    'description' : 'Use this domain for words referring to an area.',
    'value' : '8.5.4 Area'
  },
  {
    'guid' : 'ec6f626c-e7a0-4ec7-a541-d683f20c9271',
    'id' : '8.5.4.1',
    'code' : '8.5.4.1',
    'abbr' : '8.5.4.1',
    'name' : { 'en': 'Vicinity' },
    'description' : 'Use this domain for words related to a vicinity--an area around something else.',
    'value' : '8.5.4.1 Vicinity'
  },
  {
    'guid' : '80de758c-537d-4c8c-8308-4babfb2c787a',
    'id' : '8.5.4.2',
    'code' : '8.5.4.2',
    'abbr' : '8.5.4.2',
    'name' : { 'en': 'Occupy an area' },
    'description' : 'Use this domain for words related to occupying an area.',
    'value' : '8.5.4.2 Occupy an area'
  },
  {
    'guid' : 'f45a456e-8e23-4364-8842-047cc73f529b',
    'id' : '8.5.4.3',
    'code' : '8.5.4.3',
    'abbr' : '8.5.4.3',
    'name' : { 'en': 'Space, room' },
    'description' : 'Use this domain for words referring to the amount of empty space that is available to be used.',
    'value' : '8.5.4.3 Space, room'
  },
  {
    'guid' : '061749a8-e28a-461c-bf2d-052ab3e157d5',
    'id' : '8.5.4.4',
    'code' : '8.5.4.4',
    'abbr' : '8.5.4.4',
    'name' : { 'en': 'Interval, space' },
    'description' : 'Use this domain for words related to an interval or space between things.',
    'value' : '8.5.4.4 Interval, space'
  },
  {
    'guid' : '3785d9f3-0922-4d79-a0fa-b97c4a26fe17',
    'id' : '8.5.5',
    'code' : '8.5.5',
    'abbr' : '8.5.5',
    'name' : { 'en': 'Spatial relations' },
    'description' : 'Use this domain for words that indicate a spatial relation between situations.',
    'value' : '8.5.5 Spatial relations'
  },
  {
    'guid' : '8fbed974-7d25-44c3-80cb-7d02e4069007',
    'id' : '8.5.6',
    'code' : '8.5.6',
    'abbr' : '8.5.6',
    'name' : { 'en': 'Contain' },
    'description' : 'Use this domain for words that express the idea that something contains something.',
    'value' : '8.5.6 Contain'
  },
  {
    'guid' : '91913920-8a6a-4ba0-9361-d6cc9e1f3639',
    'id' : '8.6',
    'code' : '8.6',
    'abbr' : '8.6',
    'name' : { 'en': 'Parts of things' },
    'description' : 'Use this domain for words that refer to a part of something. These words are often based on the parts of a person"s body.',
    'value' : '8.6 Parts of things'
  },
  {
    'guid' : 'bddc70ea-d46f-4e4b-83a1-a47bea858dd6',
    'id' : '8.6.1',
    'code' : '8.6.1',
    'abbr' : '8.6.1',
    'name' : { 'en': 'Front' },
    'description' : 'Use this domain for words related to the front part of something.',
    'value' : '8.6.1 Front'
  },
  {
    'guid' : 'ed976bbe-4fb2-4365-b136-d2fce077a73f',
    'id' : '8.6.1.1',
    'code' : '8.6.1.1',
    'abbr' : '8.6.1.1',
    'name' : { 'en': 'Back' },
    'description' : 'Use this domain for words related to the back part of something.',
    'value' : '8.6.1.1 Back'
  },
  {
    'guid' : '5ee64d88-c462-4505-b14a-5d36e357a024',
    'id' : '8.6.2',
    'code' : '8.6.2',
    'abbr' : '8.6.2',
    'name' : { 'en': 'Top' },
    'description' : 'Use this domain for words related to the top part of something.',
    'value' : '8.6.2 Top'
  },
  {
    'guid' : '6a4f5638-388e-4c8e-9bb7-8e742dac43db',
    'id' : '8.6.2.1',
    'code' : '8.6.2.1',
    'abbr' : '8.6.2.1',
    'name' : { 'en': 'Bottom' },
    'description' : 'Use this domain for words related to the bottom part of something.',
    'value' : '8.6.2.1 Bottom'
  },
  {
    'guid' : '9e775794-3dda-4942-b6af-087ffd57f342',
    'id' : '8.6.3',
    'code' : '8.6.3',
    'abbr' : '8.6.3',
    'name' : { 'en': 'Side' },
    'description' : 'Use this domain for words related to the side part of something.',
    'value' : '8.6.3 Side'
  },
  {
    'guid' : '4d2a247e-4925-4750-8c39-e2d78665d33c',
    'id' : '8.6.4',
    'code' : '8.6.4',
    'abbr' : '8.6.4',
    'name' : { 'en': 'Inner part' },
    'description' : 'Use this domain for words related to the inside part of something.',
    'value' : '8.6.4 Inner part'
  },
  {
    'guid' : 'e2806bed-b450-4469-900a-1afa7ded2224',
    'id' : '8.6.4.1',
    'code' : '8.6.4.1',
    'abbr' : '8.6.4.1',
    'name' : { 'en': 'Outer part' },
    'description' : 'Use this domain for words related to the outside or surface part of something.',
    'value' : '8.6.4.1 Outer part'
  },
  {
    'guid' : '3005971d-de4d-401f-8400-b25de5e052ad',
    'id' : '8.6.5',
    'code' : '8.6.5',
    'abbr' : '8.6.5',
    'name' : { 'en': 'Middle' },
    'description' : 'Use this domain for words related to the middle part or center of something.',
    'value' : '8.6.5 Middle'
  },
  {
    'guid' : 'fc170f70-520e-4f3e-b8b8-98e4b898fd24',
    'id' : '8.6.6',
    'code' : '8.6.6',
    'abbr' : '8.6.6',
    'name' : { 'en': 'Edge' },
    'description' : 'Use this domain for words related to the edge of something--the part of something where two sides come together.',
    'value' : '8.6.6 Edge'
  },
  {
    'guid' : 'e1e0b800-85ad-4886-abe5-9f67c022a5ed',
    'id' : '8.6.7',
    'code' : '8.6.7',
    'abbr' : '8.6.7',
    'name' : { 'en': 'End, point' },
    'description' : 'Use this domain for words related to the end of something.',
    'value' : '8.6.7 End, point'
  },
  {
    'guid' : '349f0278-7998-422a-9c3b-6053989cbb20',
    'id' : '9',
    'code' : '9',
    'abbr' : '9',
    'name' : { 'en': 'Grammar' },
    'description' : 'Use this domain for technical linguistic terms that refer to grammatical words and constructions. Most languages have few if any words in this domain.',
    'value' : '9 Grammar'
  },
  {
    'guid' : '3a2c0773-6b3e-4f8b-909d-c8f84d66d4f4',
    'id' : '9.1',
    'code' : '9.1',
    'abbr' : '9.1',
    'name' : { 'en': 'General words' },
    'description' : 'Use the following section for words that don"t belong in any other domain because they are so general in meaning that you can use them to talk about any topic. Use this domain for general and indefinite words that can be used in the place of any word. Some languages have a general word that can replace a noun or a verb. For instance some Philippine languages use the word "kwan" in this way. Colloquial German can use the word "dings" as a noun or verb. Often these words are used when you can"t remember the particular word you are trying to think of. In English we use the word "blank" when we don"t want to say a word, for instance when we are testing someone and want them to say the word.',
    'value' : '9.1 General words'
  },
  {
    'guid' : 'a72ca6f7-e389-408a-8276-fec4d60a3a56',
    'id' : '9.1.1',
    'code' : '9.1.1',
    'abbr' : '9.1.1',
    'name' : { 'en': 'Be' },
    'description' : 'Many languages have general words that indicate some kind of state. These general words may be used with a wide variety of specific meanings. For instance in English the word "be" may be used to identify something, describe something, and many other ideas.',
    'value' : '9.1.1 Be'
  },
  {
    'guid' : '0d63adce-41dd-4873-b0bf-331d0205e65d',
    'id' : '9.1.1.1',
    'code' : '9.1.1.1',
    'abbr' : '9.1.1.1',
    'name' : { 'en': 'Exist' },
    'description' : 'Use this domain for words indicating that something exists.',
    'value' : '9.1.1.1 Exist'
  },
  {
    'guid' : '093eeea2-4ff6-4ee8-ad05-8af1702b7246',
    'id' : '9.1.1.2',
    'code' : '9.1.1.2',
    'abbr' : '9.1.1.2',
    'name' : { 'en': 'Become, change state' },
    'description' : 'Many languages have general words that indicate some kind of change of state. These general words may be used with a wide variety of specific meanings. For instance in English the word "become" may be used to a change in identity, a change in characteristic, a change in nature, and many other ideas.',
    'value' : '9.1.1.2 Become, change state'
  },
  {
    'guid' : 'da988f73-fc9d-4a23-b70d-22299a7c6097',
    'id' : '9.1.1.3',
    'code' : '9.1.1.3',
    'abbr' : '9.1.1.3',
    'name' : { 'en': 'Have, of' },
    'description' : 'Many languages have several general words that are used to indicate a variety of relationships between two things. There are three such words in English:\n"have," "of," and the possessive suffix "-"s." The basic meaning of these words in English is "to own", but they can mean many other things too. For instance they can mean that I am related to someone (I have a brother), something has a part (birds have wings), and many other ideas. There is also a set of pronouns in English that are like nouns ending in -"s (my/mine, your/yours, his, her/hers, its, our/ours, their/theirs, whose). Use this domain for these general words.',
    'value' : '9.1.1.3 Have, of'
  },
  {
    'guid' : '94cad4ca-c2ec-4ff3-b9b1-11107549941d',
    'id' : '9.1.1.4',
    'code' : '9.1.1.4',
    'abbr' : '9.1.1.4',
    'name' : { 'en': 'Attribution' },
    'description' : 'Attributes often belong to a class of attributes (shape = straight, curved) or to a scale (length = long, short). The class or scale can sometimes be included in the expression, but does not mark the proposition itself. (The towel <feels> damp. The box <weighs> five kilos.)',
    'value' : '9.1.1.4 Attribution'
  },
  {
    'guid' : 'e28f3f79-d4a5-402c-8a70-196856791078',
    'id' : '9.1.2',
    'code' : '9.1.2',
    'abbr' : '9.1.2',
    'name' : { 'en': 'Do' },
    'description' : 'Use this domain for general verbs with a volitional subject (agent).',
    'value' : '9.1.2 Do'
  },
  {
    'guid' : 'b158fe11-5af2-4467-bbc0-cc1aee766592',
    'id' : '9.1.2.1',
    'code' : '9.1.2.1',
    'abbr' : '9.1.2.1',
    'name' : { 'en': 'Happen' },
    'description' : 'Use this domain for non-volitional pro-verbs.',
    'value' : '9.1.2.1 Happen'
  },
  {
    'guid' : '12a028d1-d910-4011-ab9d-59be69daaf65',
    'id' : '9.1.2.2',
    'code' : '9.1.2.2',
    'abbr' : '9.1.2.2',
    'name' : { 'en': 'React, respond' },
    'description' : 'Use this domain for words referring to reacting or responding to something.',
    'value' : '9.1.2.2 React, respond'
  },
  {
    'guid' : 'd47b69e0-ab4a-4111-aec3-2c889a4e70b3',
    'id' : '9.1.2.3',
    'code' : '9.1.2.3',
    'abbr' : '9.1.2.3',
    'name' : { 'en': 'Create' },
    'description' : 'Use this domain for words referring to creating something--causing something to be that did not exist before.',
    'value' : '9.1.2.3 Create'
  },
  {
    'guid' : 'ecaff061-6a12-4ad6-b818-9b140a9a3e11',
    'id' : '9.1.2.4',
    'code' : '9.1.2.4',
    'abbr' : '9.1.2.4',
    'name' : { 'en': 'Design' },
    'description' : 'Use this domain for words referring to designing something--to decide and plan how something new will look and work.',
    'value' : '9.1.2.4 Design'
  },
  {
    'guid' : '2d5d634e-75b5-4921-922e-573a809a49f8',
    'id' : '9.1.2.5',
    'code' : '9.1.2.5',
    'abbr' : '9.1.2.5',
    'name' : { 'en': 'Make' },
    'description' : 'Use this domain for words referring to making something--joining things together to create something to be that did not exist before.',
    'value' : '9.1.2.5 Make'
  },
  {
    'guid' : '10b6c417-d020-4318-a44a-ae69ea3eec5a',
    'id' : '9.1.2.6',
    'code' : '9.1.2.6',
    'abbr' : '9.1.2.6',
    'name' : { 'en': 'Change something' },
    'description' : 'Use this domain for words referring to someone changing something.',
    'value' : '9.1.2.6 Change something'
  },
  {
    'guid' : '754ac437-2841-48c3-bbb0-7d6dff52605e',
    'id' : '9.1.2.7',
    'code' : '9.1.2.7',
    'abbr' : '9.1.2.7',
    'name' : { 'en': 'Event propositions' },
    'description' : 'Use this domain for words that indicate event propositions. Event propositions are similar in that they are normally expressed by a subject and a verb, possibly including an object, indirect object, or complement clause. However there are multiple ways in which a language can express an event, such as a passive construction, noun phrase, or subordinate clause. In addition each event type is different in its primary cases, and in the ways those cases are marked. Each event type has subtypes, such as intransitive, transitive, and bitransitive verbs. A great deal of research is needed in order to identify all the variations. Ultimately every verb must be investigated to determine how it behaves in each syntactic construction and how its case relations are marked. No two verbs are entirely alike.',
    'value' : '9.1.2.7 Event propositions'
  },
  {
    'guid' : '0037693a-ae42-4e5c-85f5-10a05482d4ee',
    'id' : '9.1.3',
    'code' : '9.1.3',
    'abbr' : '9.1.3',
    'name' : { 'en': 'Thing' },
    'description' : 'Use this domain for general words referring to things.',
    'value' : '9.1.3 Thing'
  },
  {
    'guid' : 'c9741b97-ad50-465c-a4ca-b21d488f45fe',
    'id' : '9.1.3.1',
    'code' : '9.1.3.1',
    'abbr' : '9.1.3.1',
    'name' : { 'en': 'Physical, non-physical' },
    'description' : 'Use this domain for words describing something that is physical--that you can touch and see, and for words describing something that is non-physical--that you cannot touch or see.',
    'value' : '9.1.3.1 Physical, non-physical'
  },
  {
    'guid' : '74cd7314-5ef6-4505-a35a-81468b5a3f3a',
    'id' : '9.1.3.2',
    'code' : '9.1.3.2',
    'abbr' : '9.1.3.2',
    'name' : { 'en': 'Situation' },
    'description' : 'Use this domain for words referring to a situation--a particular time and place, and the things that are true about it.',
    'value' : '9.1.3.2 Situation'
  },
  {
    'guid' : '316f27aa-ed6d-4bc3-9d14-840946a6f4e9',
    'id' : '9.1.4',
    'code' : '9.1.4',
    'abbr' : '9.1.4',
    'name' : { 'en': 'General adjectives' },
    'description' : 'Use this domain for general adjectives that can replace or stand for a specific adjective.',
    'value' : '9.1.4 General adjectives'
  },
  {
    'guid' : 'd9b9db39-d87e-4d04-8298-1f1b969dbda1',
    'id' : '9.1.5',
    'code' : '9.1.5',
    'abbr' : '9.1.5',
    'name' : { 'en': 'General adverbs' },
    'description' : 'Use this domain for general adverbs that can replace or stand for other adverbs.',
    'value' : '9.1.5 General adverbs'
  },
  {
    'guid' : 'af0909a5-928a-4421-baaa-f33b14302714',
    'id' : '9.2',
    'code' : '9.2',
    'abbr' : '9.2',
    'name' : { 'en': 'Part of speech' },
    'description' : 'This domain is for organization purposes and should not be used for any words. Use the domains in this section for words that belong to a particular part of speech. It is best not to use these domains, since they are based on grammar and not meaning. But if you have a small group of words that belong to a part of speech and you want to list them all, you can use these domains. You can also classify words in this section if you don"t know what they mean yet.',
    'value' : '9.2 Part of speech'
  },
  {
    'guid' : '64f39297-426b-45a8-b5d2-097cf71d688c',
    'id' : '9.2.1',
    'code' : '9.2.1',
    'abbr' : '9.2.1',
    'name' : { 'en': 'Adjectives' },
    'description' : 'Use this domain to list all adjectives. If there are many adjectives in your language, you should not try to list them all here. If you want to find all the adjectives, most dictionary programs can sort your dictionary by part of speech. However if your language only has a few adjectives, you can list them all in this domain. In the book, "Where Have All the Adjectives Gone?" R. M. W. Dixon [Dixon, R. M. W. 1982. Where have all the adjectives gone? Berlin: Mouton.] identifies seven universal semantic types that are often expressed by adjectives. They are: Age (new, young, old), Dimension (big, little, long, short, wide, narrow, thick, fat, thin), Value (good, bad, proper, perfect, excellent, fine, delicious, atrocious, poor), Color (black, white, red), Human propensity (jealous, happy, kind, clever, generous, cruel, rude, proud, wicked), Physical property (hard, soft, heavy, light, rough, smooth, hot, cold, sweet, sour), Speed (fast, slow). Words in the Human propensity class may be nouns. Words in the Physical property and Speed classes may be verbs.',
    'value' : '9.2.1 Adjectives'
  },
  {
    'guid' : '24d3d7f9-0fda-4759-930b-6b721d3e9115',
    'id' : '9.2.2',
    'code' : '9.2.2',
    'abbr' : '9.2.2',
    'name' : { 'en': 'Adverbs' },
    'description' : 'Use this domain to list all adverbs. If there are many adverbs in your language, it is probably not worth the trouble to list them here. The Shoebox program (and other dictionary programs) can sort your dictionary by part of speech.',
    'value' : '9.2.2 Adverbs'
  },
  {
    'guid' : '0296465a-25de-4af6-a122-376956b4b452',
    'id' : '9.2.3',
    'code' : '9.2.3',
    'abbr' : '9.2.3',
    'name' : { 'en': 'Pronouns' },
    'description' : 'Use this domain for the personal pronouns, including independent, subject, object, and possessive pronouns. It is best to collect all the pronouns in a chart. This way you are more certain of collecting them all and seeing how they are related to each other. A language may have more sets and more distinctions than English does, or it may have less. For instance some languages have a pronoun "we" which includes the hearer, and another pronoun "we" which excludes the hearer. Other languages have an indefinite pronoun that means something like the English word "someone". Many languages do not have the masculine (he), feminine (she), and neuter (it) distinctions that English has. It is necessary to determine the sets and functions of the pronouns for each language.',
    'value' : '9.2.3 Pronouns'
  },
  {
    'guid' : '13df6ee2-4189-4faa-b54d-768588d03978',
    'id' : '9.2.3.1',
    'code' : '9.2.3.1',
    'abbr' : '9.2.3.1',
    'name' : { 'en': 'Reflexive pronouns' },
    'description' : 'Use this domain for pronouns that refer back to the subject of the sentence. These pronouns should be added to the chart of personal pronouns.',
    'value' : '9.2.3.1 Reflexive pronouns'
  },
  {
    'guid' : 'd4521b0f-0703-48cc-94a0-f42ccc09959c',
    'id' : '9.2.3.2',
    'code' : '9.2.3.2',
    'abbr' : '9.2.3.2',
    'name' : { 'en': 'Indefinite pronouns' },
    'description' : 'Use this domain for pronouns that do not refer to a definite person or thing, but can refer to anyone or anything. Some languages will not have all the sets of pronouns described below. Add each set you find in your language to the pronoun chart.',
    'value' : '9.2.3.2 Indefinite pronouns'
  },
  {
    'guid' : '8db17eef-6c42-4ba0-9f07-a3b0e7c8f1e1',
    'id' : '9.2.3.3',
    'code' : '9.2.3.3',
    'abbr' : '9.2.3.3',
    'name' : { 'en': 'Relative pronouns' },
    'description' : 'Use this domain for pronouns used in relative clauses.',
    'value' : '9.2.3.3 Relative pronouns'
  },
  {
    'guid' : '76795fdd-55dc-4fb7-a9ad-d1423c31df50',
    'id' : '9.2.3.4',
    'code' : '9.2.3.4',
    'abbr' : '9.2.3.4',
    'name' : { 'en': 'Question words' },
    'description' : 'Use this domain for pronouns used in questions.',
    'value' : '9.2.3.4 Question words'
  },
  {
    'guid' : 'ad4d28f0-5cbb-4b82-b736-9b41860a248c',
    'id' : '9.2.3.5',
    'code' : '9.2.3.5',
    'abbr' : '9.2.3.5',
    'name' : { 'en': 'Demonstrative pronouns' },
    'description' : 'Use this domain for demonstrative pronouns.',
    'value' : '9.2.3.5 Demonstrative pronouns'
  },
  {
    'guid' : 'a139558a-8df9-4fc9-bd3e-816a1408ba7f',
    'id' : '9.2.3.6',
    'code' : '9.2.3.6',
    'abbr' : '9.2.3.6',
    'name' : { 'en': 'Personally' },
    'description' : 'Use this domain for words that indicate that someone does something himself, rather than through someone else.',
    'value' : '9.2.3.6 Personally'
  },
  {
    'guid' : '811a2abc-4bf3-44e4-9ff4-e33ff4470ce6',
    'id' : '9.2.4',
    'code' : '9.2.4',
    'abbr' : '9.2.4',
    'name' : { 'en': 'Prepositions, postpositions' },
    'description' : 'Use this domain to list all prepositions and postpositions.',
    'value' : '9.2.4 Prepositions, postpositions'
  },
  {
    'guid' : 'f950b7cc-fb85-4dbb-b8ca-934d38cae7fc',
    'id' : '9.2.5',
    'code' : '9.2.5',
    'abbr' : '9.2.5',
    'name' : { 'en': 'Conjunctions' },
    'description' : 'Use this domain to list all conjunctions.',
    'value' : '9.2.5 Conjunctions'
  },
  {
    'guid' : 'a42aa891-e4fd-489e-b573-b9d20dfc5c2a',
    'id' : '9.2.5.1',
    'code' : '9.2.5.1',
    'abbr' : '9.2.5.1',
    'name' : { 'en': 'Phrase conjunctions' },
    'description' : 'Use this domain to list all phrase level conjunctions--conjunctions that join two words within a phrase.',
    'value' : '9.2.5.1 Phrase conjunctions'
  },
  {
    'guid' : '2c576c40-17ae-45a7-9ec8-6c16e02ab9c3',
    'id' : '9.2.5.2',
    'code' : '9.2.5.2',
    'abbr' : '9.2.5.2',
    'name' : { 'en': 'Clause conjunctions' },
    'description' : 'Use this domain to list all clause level conjunctions--conjunctions that join two clauses.',
    'value' : '9.2.5.2 Clause conjunctions'
  },
  {
    'guid' : 'ae04020a-3bb2-4672-ad75-71ce72d461ea',
    'id' : '9.2.5.3',
    'code' : '9.2.5.3',
    'abbr' : '9.2.5.3',
    'name' : { 'en': 'Sentence conjunctions' },
    'description' : 'Use this domain to list all sentence level conjunctions--conjunctions that join two sentences.',
    'value' : '9.2.5.3 Sentence conjunctions'
  },
  {
    'guid' : 'a7b32d1b-1be7-43ec-94a1-fc7bdd826168',
    'id' : '9.2.6',
    'code' : '9.2.6',
    'abbr' : '9.2.6',
    'name' : { 'en': 'Particles' },
    'description' : 'Use this domain to list all particles.',
    'value' : '9.2.6 Particles'
  },
  {
    'guid' : 'da41ea1f-dd09-421d-a1a5-174ff43f4eff',
    'id' : '9.2.6.1',
    'code' : '9.2.6.1',
    'abbr' : '9.2.6.1',
    'name' : { 'en': 'Classifiers' },
    'description' : 'Use this domain to list all classifiers.',
    'value' : '9.2.6.1 Classifiers'
  },
  {
    'guid' : 'b34dc5c9-3367-4bfc-b077-cd014250dc5c',
    'id' : '9.2.7',
    'code' : '9.2.7',
    'abbr' : '9.2.7',
    'name' : { 'en': 'Interjections' },
    'description' : 'Use this domain to list all interjections.',
    'value' : '9.2.7 Interjections'
  },
  {
    'guid' : '8206415e-a915-4842-a46a-fbea64f1a0e3',
    'id' : '9.2.8',
    'code' : '9.2.8',
    'abbr' : '9.2.8',
    'name' : { 'en': 'Idiophones' },
    'description' : 'Use this domain to list all idiophones. If there are many idiophones in your language, it is probably not worth the trouble to list them here. The Shoebox program\n(and other dictionary programs) can sort your dictionary by part of speech.',
    'value' : '9.2.8 Idiophones'
  },
  {
    'guid' : '35624f3a-2029-43b3-b70a-83e63ac9052f',
    'id' : '9.2.9',
    'code' : '9.2.9',
    'abbr' : '9.2.9',
    'name' : { 'en': 'Affixes' },
    'description' : 'Use this domain to list all affixes that do not fit in any of the subdomains under it. This section should be filled out by a linguist.',
    'value' : '9.2.9 Affixes'
  },
  {
    'guid' : '0049664f-0931-487b-ab3c-ce11e134ce7a',
    'id' : '9.2.9.1',
    'code' : '9.2.9.1',
    'abbr' : '9.2.9.1',
    'name' : { 'en': 'Verb affixes' },
    'description' : 'Use this domain to list all verb affixes.',
    'value' : '9.2.9.1 Verb affixes'
  },
  {
    'guid' : 'a4f4943f-ad94-4736-bf5d-f8a3cb15919f',
    'id' : '9.2.9.2',
    'code' : '9.2.9.2',
    'abbr' : '9.2.9.2',
    'name' : { 'en': 'Noun affixes' },
    'description' : 'Use this domain to list all noun affixes.',
    'value' : '9.2.9.2 Noun affixes'
  },
  {
    'guid' : '751f726b-b7cd-470e-a9fd-f2f1b460dd0d',
    'id' : '9.2.9.3',
    'code' : '9.2.9.3',
    'abbr' : '9.2.9.3',
    'name' : { 'en': 'Derivational affixes' },
    'description' : 'Use this domain to list all derivational affixes. A derivational affix is joined to a root and changes it into a different word. Derivational affixes often change the root into a different part of speech. Adding a derivational affix usually changes the meaning of the root in a significant way.',
    'value' : '9.2.9.3 Derivational affixes'
  },
  {
    'guid' : '5422d4ba-8af4-4767-912e-43b60ef28eab',
    'id' : '9.3',
    'code' : '9.3',
    'abbr' : '9.3',
    'name' : { 'en': 'Very' },
    'description' : 'Use this domain for words that intensify an attribute.',
    'value' : '9.3 Very'
  },
  {
    'guid' : 'aaf9d375-f0a0-4c7b-bbf8-3c7ffd4f5a52',
    'id' : '9.3.1',
    'code' : '9.3.1',
    'abbr' : '9.3.1',
    'name' : { 'en': 'Degree' },
    'description' : 'Use this domain for words that indicate a degree on a scale.',
    'value' : '9.3.1 Degree'
  },
  {
    'guid' : '7f91aa6d-f342-4fb9-9448-69d694cda9c5',
    'id' : '9.3.1.1',
    'code' : '9.3.1.1',
    'abbr' : '9.3.1.1',
    'name' : { 'en': 'To a large degree' },
    'description' : 'Use this domain for words referring to a large degree.',
    'value' : '9.3.1.1 To a large degree'
  },
  {
    'guid' : '1082c52b-490a-4eec-acf1-7016796dafd9',
    'id' : '9.3.1.2',
    'code' : '9.3.1.2',
    'abbr' : '9.3.1.2',
    'name' : { 'en': 'To a small degree' },
    'description' : 'Use this domain for words referring to a small degree.',
    'value' : '9.3.1.2 To a small degree'
  },
  {
    'guid' : '73b959ab-0229-4710-af99-dfc9b5370540',
    'id' : '9.3.1.3',
    'code' : '9.3.1.3',
    'abbr' : '9.3.1.3',
    'name' : { 'en': 'To a larger degree' },
    'description' : 'Use this domain for words referring to a larger degree.',
    'value' : '9.3.1.3 To a larger degree'
  },
  {
    'guid' : '984dc2b7-6fdd-4257-abdc-5873abb7bb70',
    'id' : '9.3.1.4',
    'code' : '9.3.1.4',
    'abbr' : '9.3.1.4',
    'name' : { 'en': 'To a smaller degree' },
    'description' : 'Use this domain for words referring to a smaller degree.',
    'value' : '9.3.1.4 To a smaller degree'
  },
  {
    'guid' : 'e1dd83dd-955a-4bc7-a761-fc91555da1f8',
    'id' : '9.3.2',
    'code' : '9.3.2',
    'abbr' : '9.3.2',
    'name' : { 'en': 'Completely' },
    'description' : 'Use this domain for words referring to a complete degree--when something is done, happens, is thought, is felt, etc completely and in every way.',
    'value' : '9.3.2 Completely'
  },
  {
    'guid' : 'd74914e7-e329-49d4-8513-ec8d850241e4',
    'id' : '9.3.3',
    'code' : '9.3.3',
    'abbr' : '9.3.3',
    'name' : { 'en': 'Partly' },
    'description' : 'Use this domain for words referring to a complete degree--when something is done, happens, is thought, is felt, etc completely and in every way.',
    'value' : '9.3.3 Partly'
  },
  {
    'guid' : 'a22d5c1c-daed-4e7a-8243-493f2d841314',
    'id' : '9.3.4',
    'code' : '9.3.4',
    'abbr' : '9.3.4',
    'name' : { 'en': 'Do intensely' },
    'description' : 'Use this domain for words indicating intensity of an action.',
    'value' : '9.3.4 Do intensely'
  },
  {
    'guid' : 'c7c3aa7d-a4b5-45af-9a31-a640179e8fa4',
    'id' : '9.3.5',
    'code' : '9.3.5',
    'abbr' : '9.3.5',
    'name' : { 'en': 'Attribution of an attribute' },
    'description' : 'Use this domain for words that modify an attribute.',
    'value' : '9.3.5 Attribution of an attribute'
  },
  {
    'guid' : '61a28bdc-c05c-49d8-b47e-d54a9082156c',
    'id' : '9.4',
    'code' : '9.4',
    'abbr' : '9.4',
    'name' : { 'en': 'Semantic constituents related to verbs' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that modify verbs.',
    'value' : '9.4 Semantic constituents related to verbs'
  },
  {
    'guid' : '793993ac-20c1-49f0-9716-e4cdc7da4439',
    'id' : '9.4.1',
    'code' : '9.4.1',
    'abbr' : '9.4.1',
    'name' : { 'en': 'Tense and aspect' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate tense and aspect.',
    'value' : '9.4.1 Tense and aspect'
  },
  {
    'guid' : 'df2ee830-0668-43d7-8a32-e2fd3e7b31d8',
    'id' : '9.4.1.1',
    'code' : '9.4.1.1',
    'abbr' : '9.4.1.1',
    'name' : { 'en': 'Tense' },
    'description' : 'Use this domain for verbal auxiliaries, affixes, adverbs, and particles that indicate tense (also known as temporal deixis)--the time of a situation (event, activity, or state) in relation to a reference point, which is usually the time of utterance. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.1.1 Tense'
  },
  {
    'guid' : 'ccbbd16f-58c5-45c1-bfff-1fba64d9740e',
    'id' : '9.4.1.2',
    'code' : '9.4.1.2',
    'abbr' : '9.4.1.2',
    'name' : { 'en': 'Aspect--dynamic verbs' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate aspects of dynamic verbs. Aspects describe the temporal contours of a situation. They may be combined with any of the tenses, either in the same morpheme or in combinations of morphemes. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.1.2 Aspect--dynamic verbs'
  },
  {
    'guid' : '321d0a74-705f-40bf-8d24-809f65bee895',
    'id' : '9.4.1.3',
    'code' : '9.4.1.3',
    'abbr' : '9.4.1.3',
    'name' : { 'en': 'Aspect--stative verbs' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate aspects of stative verbs. Aspects describe the temporal contours of a situation. They may be combined with any of the tenses, either in the same morpheme or in combinations of morphemes. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.1.3 Aspect--stative verbs'
  },
  {
    'guid' : '54f59b23-a2e8-4bfc-9da2-7dd7c37d2a47',
    'id' : '9.4.1.4',
    'code' : '9.4.1.4',
    'abbr' : '9.4.1.4',
    'name' : { 'en': 'Relational tenses' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate relational tenses. Relational tenses describe situations where the reference time is not the same as the moment of speech. They may be combined with any of the tenses, either in the same morpheme or in combinations of morphemes. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.1.4 Relational tenses'
  },
  {
    'guid' : '7bf05f4e-909f-40ca-b742-9be21eba9fbb',
    'id' : '9.4.2',
    'code' : '9.4.2',
    'abbr' : '9.4.2',
    'name' : { 'en': 'Agent-oriented modalities' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate agent-oriented modalities. Agent-oriented modalities describe internal or external conditions on a willful agent with respect to the completion of the predicate situation. They may be combined with any of the tenses, either in the same morpheme or in combinations of morphemes. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.2 Agent-oriented modalities'
  },
  {
    'guid' : '59d936f3-dffb-4585-80e0-eaf6cd6a8026',
    'id' : '9.4.2.1',
    'code' : '9.4.2.1',
    'abbr' : '9.4.2.1',
    'name' : { 'en': 'Can' },
    'description' : 'Use this domain for words indicating that someone can do something.',
    'value' : '9.4.2.1 Can'
  },
  {
    'guid' : 'ab8d8dc9-eeb7-41ff-93a9-cbbd50b89a73',
    'id' : '9.4.2.2',
    'code' : '9.4.2.2',
    'abbr' : '9.4.2.2',
    'name' : { 'en': 'Can"t' },
    'description' : 'Use this domain for words related to being incapable of doing something.',
    'value' : '9.4.2.2 Can"t'
  },
  {
    'guid' : '47feee3e-80e1-469a-911c-0c550b37a2f8',
    'id' : '9.4.2.3',
    'code' : '9.4.2.3',
    'abbr' : '9.4.2.3',
    'name' : { 'en': 'Necessary ' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he thinks something must happen.',
    'value' : '9.4.2.3 Necessary '
  },
  {
    'guid' : 'c3ddfc77-e3a6-450e-a853-111f5595df87',
    'id' : '9.4.3',
    'code' : '9.4.3',
    'abbr' : '9.4.3',
    'name' : { 'en': 'Moods' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate moods.',
    'value' : '9.4.3 Moods'
  },
  {
    'guid' : '965c6a0b-3034-4e2f-a00b-ed2eb3119a5d',
    'id' : '9.4.3.1',
    'code' : '9.4.3.1',
    'abbr' : '9.4.3.1',
    'name' : { 'en': 'Imperative ' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate imperatives. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press. Use this domain for words and affixes that a speaker uses to indicate that he is making a command. English has no command word. Some languages change the form of the verb by adding an affix. Some languages have special verbs that are only or normally used as commands. Those verbs could be classified here.',
    'value' : '9.4.3.1 Imperative '
  },
  {
    'guid' : 'edeb9458-3bdb-4d14-aaa1-6eb457307b9c',
    'id' : '9.4.3.2',
    'code' : '9.4.3.2',
    'abbr' : '9.4.3.2',
    'name' : { 'en': 'Hortative' },
    'description' : 'Use this domain for ways of saying that someone should do something. If I say someone should do something, I think it is good that he does it.',
    'value' : '9.4.3.2 Hortative'
  },
  {
    'guid' : '6fa33de6-00f4-44d5-b6b3-b3c4a4f671e5',
    'id' : '9.4.3.3',
    'code' : '9.4.3.3',
    'abbr' : '9.4.3.3',
    'name' : { 'en': 'Interrogative ' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he is asking a question. English has no question word, but other languages such as Japanese do.',
    'value' : '9.4.3.3 Interrogative '
  },
  {
    'guid' : '3ea4c495-b837-4310-8741-38d89fa63e0b',
    'id' : '9.4.4',
    'code' : '9.4.4',
    'abbr' : '9.4.4',
    'name' : { 'en': 'Epistemic moods' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate epistemic moods. Epistemic moods have the whole proposition in their scope and indicate the degree of commitment of the speaker to the truth or future truth of the proposition. They may be combined with any of the tenses, either in the same morpheme or in combinations of morphemes. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.4 Epistemic moods'
  },
  {
    'guid' : 'ad7cc381-1dc6-4fc2-94a4-acd5bf7a11da',
    'id' : '9.4.4.1',
    'code' : '9.4.4.1',
    'abbr' : '9.4.4.1',
    'name' : { 'en': 'Certainly, definitely' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he thinks something is certainly true or is certain to happen.',
    'value' : '9.4.4.1 Certainly, definitely'
  },
  {
    'guid' : 'af6fe2d6-576d-473f-8a32-583779d95d1d',
    'id' : '9.4.4.2',
    'code' : '9.4.4.2',
    'abbr' : '9.4.4.2',
    'name' : { 'en': 'Sure' },
    'description' : 'Use this domain for words related to being sure that something is true.',
    'value' : '9.4.4.2 Sure'
  },
  {
    'guid' : '84eb31d3-b932-4b3b-b945-85884ea856c7',
    'id' : '9.4.4.3',
    'code' : '9.4.4.3',
    'abbr' : '9.4.4.3',
    'name' : { 'en': 'Probably ' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he thinks something is probable or likely to occur.',
    'value' : '9.4.4.3 Probably '
  },
  {
    'guid' : '85c5b8f7-8086-493d-b70d-a361bfa56f09',
    'id' : '9.4.4.4',
    'code' : '9.4.4.4',
    'abbr' : '9.4.4.4',
    'name' : { 'en': 'Possible' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he thinks something is possible. Maybe implies that the speaker doesn"t know something.',
    'value' : '9.4.4.4 Possible'
  },
  {
    'guid' : 'e0c32642-7c51-4e23-a776-f63f2f2f936d',
    'id' : '9.4.4.5',
    'code' : '9.4.4.5',
    'abbr' : '9.4.4.5',
    'name' : { 'en': 'Uncertain' },
    'description' : 'Use this domain for words that indicate that no one is certain that something is true, or when it is impossible to be certain that something is true.',
    'value' : '9.4.4.5 Uncertain'
  },
  {
    'guid' : '1a635032-6e13-4a56-aa03-6c6a015c502e',
    'id' : '9.4.4.6',
    'code' : '9.4.4.6',
    'abbr' : '9.4.4.6',
    'name' : { 'en': 'Unsure' },
    'description' : 'Use this domain for words related to not feeling sure about something or someone.',
    'value' : '9.4.4.6 Unsure'
  },
  {
    'guid' : '13e67cc9-055b-4f9b-9217-a16b18db0329',
    'id' : '9.4.4.6.1',
    'code' : '9.4.4.6.1',
    'abbr' : '9.4.4.6.1',
    'name' : { 'en': 'Think so' },
    'description' : 'Use this domain for words indicating that you think something is true, but you are not completely sure about it.',
    'value' : '9.4.4.6.1 Think so'
  },
  {
    'guid' : 'e0b00a13-8648-4635-afe5-0be3c0b6a05c',
    'id' : '9.4.4.6.2',
    'code' : '9.4.4.6.2',
    'abbr' : '9.4.4.6.2',
    'name' : { 'en': 'Maybe' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he thinks it is possible that something may happen or be true, but he isn"t certain.',
    'value' : '9.4.4.6.2 Maybe'
  },
  {
    'guid' : '858af232-b570-4153-b4c0-f60930df9ced',
    'id' : '9.4.4.6.3',
    'code' : '9.4.4.6.3',
    'abbr' : '9.4.4.6.3',
    'name' : { 'en': 'Seem' },
    'description' : 'Use this domain for words indicating that something seems to be a certain way--you see (or hear) something and think something about it, but you are not sure that what you think is true.',
    'value' : '9.4.4.6.3 Seem'
  },
  {
    'guid' : '7c022751-a9f9-412d-8b27-8cd03b797e2d',
    'id' : '9.4.4.7',
    'code' : '9.4.4.7',
    'abbr' : '9.4.4.7',
    'name' : { 'en': 'Just, almost not' },
    'description' : 'Use this domain for words indicating that although something is true, it almost is not true.',
    'value' : '9.4.4.7 Just, almost not'
  },
  {
    'guid' : 'ca495e57-a8e0-4294-bfe3-7b7995dc96c7',
    'id' : '9.4.4.8',
    'code' : '9.4.4.8',
    'abbr' : '9.4.4.8',
    'name' : { 'en': 'Don"t think so, doubt it' },
    'description' : 'Use this domain for words indicating that you think something is unlikely to be true or to happen.',
    'value' : '9.4.4.8 Don"t think so, doubt it'
  },
  {
    'guid' : 'f883266a-146a-41c7-b1db-85120840c3a8',
    'id' : '9.4.4.9',
    'code' : '9.4.4.9',
    'abbr' : '9.4.4.9',
    'name' : { 'en': 'Impossible' },
    'description' : 'Use this domain for words that a speaker uses to indicate that he thinks something is impossible.',
    'value' : '9.4.4.9 Impossible'
  },
  {
    'guid' : '04752883-aa3e-42a2-bd42-454e9cd99b11',
    'id' : '9.4.5',
    'code' : '9.4.5',
    'abbr' : '9.4.5',
    'name' : { 'en': 'Evidentials' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate evidentials. An evidential is when the speaker indicates the source of the information on which an assertion about a situation is based. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.5 Evidentials'
  },
  {
    'guid' : 'cf5f83be-2c19-4cf8-8cc5-53bd32b50530',
    'id' : '9.4.5.1',
    'code' : '9.4.5.1',
    'abbr' : '9.4.5.1',
    'name' : { 'en': 'Evaluator' },
    'description' : 'Use this domain for words indicating who is evaluating the proposition.',
    'value' : '9.4.5.1 Evaluator'
  },
  {
    'guid' : 'a03663ca-0c66-4570-be2d-b40105cc4400',
    'id' : '9.4.6',
    'code' : '9.4.6',
    'abbr' : '9.4.6',
    'name' : { 'en': 'Yes' },
    'description' : 'Use this domain for words that affirm or agree with the truth of something, or that answer a yes/no question in the affirmative.',
    'value' : '9.4.6 Yes'
  },
  {
    'guid' : '350667ee-592b-47af-adca-14e820ec58cf',
    'id' : '9.4.6.1',
    'code' : '9.4.6.1',
    'abbr' : '9.4.6.1',
    'name' : { 'en': 'No, not' },
    'description' : 'Use this domain for words that negate or deny the truth of something, or that answer a yes/no question in the negative.',
    'value' : '9.4.6.1 No, not'
  },
  {
    'guid' : 'b4c1e05f-f741-45cc-8d13-a1f60f474325',
    'id' : '9.4.6.2',
    'code' : '9.4.6.2',
    'abbr' : '9.4.6.2',
    'name' : { 'en': 'Markers expecting an affirmative answer' },
    'description' : 'Use this domain for words indicating that an affirmative answer is expected to a question.',
    'value' : '9.4.6.2 Markers expecting an affirmative answer'
  },
  {
    'guid' : 'e64e647e-a5fb-463c-8eef-44879e2e70b2',
    'id' : '9.4.6.3',
    'code' : '9.4.6.3',
    'abbr' : '9.4.6.3',
    'name' : { 'en': 'Markers expecting a negative answer' },
    'description' : 'Use this domain for words indicating that a negative answer is expected to a question.',
    'value' : '9.4.6.3 Markers expecting a negative answer'
  },
  {
    'guid' : '1de2cef5-3a2d-45c1-8cb6-06b2ac087907',
    'id' : '9.4.7',
    'code' : '9.4.7',
    'abbr' : '9.4.7',
    'name' : { 'en': 'Subordinating particles' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate a subordinate clause. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.7 Subordinating particles'
  },
  {
    'guid' : 'fa8e72a0-1bfe-4b49-a287-293b44213960',
    'id' : '9.4.8',
    'code' : '9.4.8',
    'abbr' : '9.4.8',
    'name' : { 'en': 'Adverbial clauses' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate adverbial clauses. The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.4.8 Adverbial clauses'
  },
  {
    'guid' : 'aab82dc7-de9f-44b3-845e-0c926f47cfb6',
    'id' : '9.5',
    'code' : '9.5',
    'abbr' : '9.5',
    'name' : { 'en': 'Case' },
    'description' : 'Each verb has a set of semantic case relations. For instance in the sentence "I gave flowers to my wife" the verb give has three case relations. "I" is the Agent,\n"flowers" is the Patient, and "my wife is the "Recipient". In this sentence the only word that marks a case relation is "to". English often marks case relations by their position in the sentence. Some languages mark case relations by affixes, prepositions, postpositions, and sometimes special verbs. To completely describe a language, each verb must be investigated, all its case relations must be identified, and all the ways in which these relations are marked must be described. Since verbs are often unique and unpredictable in their case relations, this information should go into the dictionary. This section should be used to classify the words and affixes that are used to mark case relations. This domain should be used for technical terms that refer to case.',
    'value' : '9.5 Case'
  },
  {
    'guid' : 'bb8ddf5f-707d-46c0-aff4-45683d26fd68',
    'id' : '9.5.1',
    'code' : '9.5.1',
    'abbr' : '9.5.1',
    'name' : { 'en': 'Primary cases' },
    'description' : 'Use this section for primary cases.',
    'value' : '9.5.1 Primary cases'
  },
  {
    'guid' : '93df663c-9a5e-48aa-984f-fdf413079bc2',
    'id' : '9.5.1.1',
    'code' : '9.5.1.1',
    'abbr' : '9.5.1.1',
    'name' : { 'en': 'Beneficiary of an event' },
    'description' : 'Use this domain for words that mark the beneficiary of an event. The sentence "John built a house for his father" is ambiguous. If the house was for his father to live in, then "for" would mark the "Beneficiary of a patient", meaning that the house was for the father. If, on the other hand, the father was intending to build the house to sell, but couldn"t due to an injury, then "for" would mark the "Beneficiary of an event", meaning the father benefited from the building of the house.',
    'value' : '9.5.1.1 Beneficiary of an event'
  },
  {
    'guid' : '4415aff1-4d74-463e-a25d-9832c7477329',
    'id' : '9.5.1.2',
    'code' : '9.5.1.2',
    'abbr' : '9.5.1.2',
    'name' : { 'en': 'Instrument' },
    'description' : 'Use this domain for words that mark an instrument used to do something.',
    'value' : '9.5.1.2 Instrument'
  },
  {
    'guid' : '4f587b2b-60a6-4ea0-9fe5-89e5a502d380',
    'id' : '9.5.1.3',
    'code' : '9.5.1.3',
    'abbr' : '9.5.1.3',
    'name' : { 'en': 'Means' },
    'description' : 'Use this domain for words indicating the means by which something is done.',
    'value' : '9.5.1.3 Means'
  },
  {
    'guid' : 'd0b14231-1471-41b3-aeb5-69199acaaefb',
    'id' : '9.5.1.4',
    'code' : '9.5.1.4',
    'abbr' : '9.5.1.4',
    'name' : { 'en': 'Way, manner' },
    'description' : 'Use this domain for words indicating the way or manner in which something is done.',
    'value' : '9.5.1.4 Way, manner'
  },
  {
    'guid' : 'aabc32ee-46e4-469f-93ad-673373cb2e8d',
    'id' : '9.5.1.5',
    'code' : '9.5.1.5',
    'abbr' : '9.5.1.5',
    'name' : { 'en': 'Attendant circumstances' },
    'description' : 'Use this domain for words indicating the attendant circumstances in which something happened.',
    'value' : '9.5.1.5 Attendant circumstances'
  },
  {
    'guid' : '71757ff0-d698-426d-a791-50c4bde6f735',
    'id' : '9.5.1.6',
    'code' : '9.5.1.6',
    'abbr' : '9.5.1.6',
    'name' : { 'en': 'Spatial location of an event' },
    'description' : 'Use this domain for words indicating the spatial location of an event.',
    'value' : '9.5.1.6 Spatial location of an event'
  },
  {
    'guid' : '400d318e-a9ef-40b4-92be-0d7e96e51d8a',
    'id' : '9.5.1.6.1',
    'code' : '9.5.1.6.1',
    'abbr' : '9.5.1.6.1',
    'name' : { 'en': 'Source (of movement)' },
    'description' : 'Use this domain for words that mark the Source (original location) of something.',
    'value' : '9.5.1.6.1 Source (of movement)'
  },
  {
    'guid' : 'a9470e53-ee43-4c87-9cce-09cc4fa6b1c1',
    'id' : '9.5.1.6.2',
    'code' : '9.5.1.6.2',
    'abbr' : '9.5.1.6.2',
    'name' : { 'en': 'Path (of movement)' },
    'description' : 'Use this domain for words indicating the Path of movement.',
    'value' : '9.5.1.6.2 Path (of movement)'
  },
  {
    'guid' : '9f91cd53-8b9e-4d76-82e4-5ede39112322',
    'id' : '9.5.1.6.3',
    'code' : '9.5.1.6.3',
    'abbr' : '9.5.1.6.3',
    'name' : { 'en': 'Goal (of movement)' },
    'description' : 'Use this domain for words indicating the Goal of movement.',
    'value' : '9.5.1.6.3 Goal (of movement)'
  },
  {
    'guid' : '66e2806d-3e16-4fb2-a158-7f3b4dd5d9af',
    'id' : '9.5.1.6.4',
    'code' : '9.5.1.6.4',
    'abbr' : '9.5.1.6.4',
    'name' : { 'en': 'Origin (of a person)' },
    'description' : 'Use this domain for words that mark the place where someone was born or the place where they have been living.',
    'value' : '9.5.1.6.4 Origin (of a person)'
  },
  {
    'guid' : '0eefa07a-e0a3-49e3-aeb4-62f1eafd8e23',
    'id' : '9.5.2',
    'code' : '9.5.2',
    'abbr' : '9.5.2',
    'name' : { 'en': 'Semantically similar events' },
    'description' : 'Use this section for words that join semantically similar events into one sentence. Each sentence is actually reporting two or more situations, which may differ in one or two respects. The words to be included in these domains indicate that two situations are being reported, or mark the differences between the two situations.',
    'value' : '9.5.2 Semantically similar events'
  },
  {
    'guid' : '42b21a6e-e2f3-4468-9e92-49ee4de6909a',
    'id' : '9.5.2.1',
    'code' : '9.5.2.1',
    'abbr' : '9.5.2.1',
    'name' : { 'en': 'Together' },
    'description' : 'Use this domain for words indicating when two or more people each do the same thing and do it together, or when they do it separately.',
    'value' : '9.5.2.1 Together'
  },
  {
    'guid' : '3a545732-145a-4034-8f72-e08d752cb4d4',
    'id' : '9.5.2.2',
    'code' : '9.5.2.2',
    'abbr' : '9.5.2.2',
    'name' : { 'en': 'With, be with' },
    'description' : 'Use this domain for words indicating a person who accompanied the subject of a proposition.',
    'value' : '9.5.2.2 With, be with'
  },
  {
    'guid' : '6903b844-79be-4e9d-a3c8-1ca2a385bf4f',
    'id' : '9.5.2.3',
    'code' : '9.5.2.3',
    'abbr' : '9.5.2.3',
    'name' : { 'en': 'With, do with someone' },
    'description' : 'Use this domain for words indicating a person who does something with another person who is the subject of the sentence.',
    'value' : '9.5.2.3 With, do with someone'
  },
  {
    'guid' : 'd39b2432-87d5-4f3e-8101-de06001b42d6',
    'id' : '9.5.2.4',
    'code' : '9.5.2.4',
    'abbr' : '9.5.2.4',
    'name' : { 'en': 'Each other' },
    'description' : 'Use this domain for words indicating that two or more people do something to each other.',
    'value' : '9.5.2.4 Each other'
  },
  {
    'guid' : '52f9a8f0-d97d-4aa1-8c2c-d907d7cb83fc',
    'id' : '9.5.2.5',
    'code' : '9.5.2.5',
    'abbr' : '9.5.2.5',
    'name' : { 'en': 'In groups' },
    'description' : 'Use this domain for words indicating that the subjects of a clause do something in groups.',
    'value' : '9.5.2.5 In groups'
  },
  {
    'guid' : '116bef13-e80f-4a15-bb0a-bb7b3794ffac',
    'id' : '9.5.3',
    'code' : '9.5.3',
    'abbr' : '9.5.3',
    'name' : { 'en': 'Patient-related cases' },
    'description' : 'Use this section for cases that bear a relationship to the "Patient" of a proposition.',
    'value' : '9.5.3 Patient-related cases'
  },
  {
    'guid' : 'ca0f9b9b-31fc-4ae6-9563-abedc4a5af98',
    'id' : '9.5.3.1',
    'code' : '9.5.3.1',
    'abbr' : '9.5.3.1',
    'name' : { 'en': 'Beneficiary (of a patient)' },
    'description' : 'Use this domain for words that mark the beneficiary of the Patient of an activity. The Patient is often expressed as the object of a sentence. In the sentence\n"John built a house for his parents," the house is the Patient. It is the house that benefits the parents, not the building of the house.',
    'value' : '9.5.3.1 Beneficiary (of a patient)'
  },
  {
    'guid' : '99c51a2c-ad49-48a6-bb0b-f059da745ec4',
    'id' : '9.5.3.2',
    'code' : '9.5.3.2',
    'abbr' : '9.5.3.2',
    'name' : { 'en': 'Recipient (of a patient)' },
    'description' : 'Use this domain for words that mark the recipient of the Patient of an activity. The Patient is usually expressed as the object of a sentence.',
    'value' : '9.5.3.2 Recipient (of a patient)'
  },
  {
    'guid' : '55b93f1c-6ce0-4d13-ae1e-f06360e4689c',
    'id' : '9.5.3.3',
    'code' : '9.5.3.3',
    'abbr' : '9.5.3.3',
    'name' : { 'en': 'With (a patient)' },
    'description' : 'Use this domain for words that mark a second Patient that accompanies the primary Patient of an activity. In this type of sentence there are actually two Patients, but one of them has more prominence than the other. The primary patient is usually expressed as the object of the sentence. The second Patient may be marked by an oblique case or preposition/postposition. For instance it may be conceived as accompanying the first Patient.',
    'value' : '9.5.3.3 With (a patient)'
  },
  {
    'guid' : '7f7fc197-5064-43c0-af51-2919fb7355c9',
    'id' : '9.6',
    'code' : '9.6',
    'abbr' : '9.6',
    'name' : { 'en': 'Connected with, related ' },
    'description' : 'Use the domains in this section for words that indicate a logical relation between two or more words or sentences. Use this domain for words that indicate an unspecified logical relation between people, things, or situations.',
    'value' : '9.6 Connected with, related '
  },
  {
    'guid' : '265f5645-94cb-485c-8bf9-0a3ab2354f63',
    'id' : '9.6.1',
    'code' : '9.6.1',
    'abbr' : '9.6.1',
    'name' : { 'en': 'Coordinate relations' },
    'description' : 'Use this section for words indicating coordinate relations. Do not put any words in this domain. It is only for organizational purposes.',
    'value' : '9.6.1 Coordinate relations'
  },
  {
    'guid' : 'c029eed8-2ec0-4f6f-aa22-3a066bb23ea6',
    'id' : '9.6.1.1',
    'code' : '9.6.1.1',
    'abbr' : '9.6.1.1',
    'name' : { 'en': 'And, also' },
    'description' : 'Use this domain for words that indicate that you are adding another thought to a previous thought. Words in this domain may indicate a variety of relationships between words, phrases, clauses, or sentences. For instance the words may join two clauses that are the same except that the subjects are different, or the objects are different, or the verbs are different.',
    'value' : '9.6.1.1 And, also'
  },
  {
    'guid' : '8b5f9519-7301-4400-b93d-ebde4ea3def8',
    'id' : '9.6.1.2',
    'code' : '9.6.1.2',
    'abbr' : '9.6.1.2',
    'name' : { 'en': 'Or, either' },
    'description' : 'Use this domain for words indicating an alternative relation between two things or propositions.',
    'value' : '9.6.1.2 Or, either'
  },
  {
    'guid' : '13edbeff-8913-49ef-8f02-777f86fb512d',
    'id' : '9.6.1.3',
    'code' : '9.6.1.3',
    'abbr' : '9.6.1.3',
    'name' : { 'en': 'Association' },
    'description' : 'Use this domain for words indicating an association between two things.',
    'value' : '9.6.1.3 Association'
  },
  {
    'guid' : 'ecf1cce7-ed58-44bf-870a-e8579b309c54',
    'id' : '9.6.1.4',
    'code' : '9.6.1.4',
    'abbr' : '9.6.1.4',
    'name' : { 'en': 'Combinative relation' },
    'description' : 'Use this domain for words indicating a combinative relation between two things.',
    'value' : '9.6.1.4 Combinative relation'
  },
  {
    'guid' : '30fff450-1aa5-4993-9c14-c8019a5f072e',
    'id' : '9.6.1.5',
    'code' : '9.6.1.5',
    'abbr' : '9.6.1.5',
    'name' : { 'en': 'But' },
    'description' : 'Use this domain for words indicating a contrast between two thoughts that are different in some way.',
    'value' : '9.6.1.5 But'
  },
  {
    'guid' : '49f45f97-95f8-4a53-8952-f90147af2ba9',
    'id' : '9.6.1.5.1',
    'code' : '9.6.1.5.1',
    'abbr' : '9.6.1.5.1',
    'name' : { 'en': 'Except' },
    'description' : 'Use this domain for words indicating that something is an exception to a group, rule or pattern--something is true of all the things (or people) in a group, but it is not true of one thing.',
    'value' : '9.6.1.5.1 Except'
  },
  {
    'guid' : 'f94e9041-49b0-4d25-aa54-9446c5ab45f4',
    'id' : '9.6.1.5.2',
    'code' : '9.6.1.5.2',
    'abbr' : '9.6.1.5.2',
    'name' : { 'en': 'Instead' },
    'description' : 'Use this domain for words indicating that something is true of one thing (or person) instead of another thing.',
    'value' : '9.6.1.5.2 Instead'
  },
  {
    'guid' : 'd8dfa6fc-84ea-4178-b4f5-95e0c113140a',
    'id' : '9.6.1.6',
    'code' : '9.6.1.6',
    'abbr' : '9.6.1.6',
    'name' : { 'en': 'Dissociation' },
    'description' : 'Use this domain for words indicating a dissociation relation between two things or propositions.',
    'value' : '9.6.1.6 Dissociation'
  },
  {
    'guid' : 'ea7c06d0-5e33-4702-b6a0-51582b216fe8',
    'id' : '9.6.1.7',
    'code' : '9.6.1.7',
    'abbr' : '9.6.1.7',
    'name' : { 'en': 'Distribution' },
    'description' : 'Use this domain for words indicating that an event is distributed throughout a group, area, or time span.',
    'value' : '9.6.1.7 Distribution'
  },
  {
    'guid' : '251b17bd-5796-43ce-ba10-54140a99a1e0',
    'id' : '9.6.1.8',
    'code' : '9.6.1.8',
    'abbr' : '9.6.1.8',
    'name' : { 'en': 'Equivalence' },
    'description' : 'Use this domain for words indicating equivalence between two things or propositions.',
    'value' : '9.6.1.8 Equivalence'
  },
  {
    'guid' : 'b2fd2d29-1389-4114-91a8-15b8d9742794',
    'id' : '9.6.2',
    'code' : '9.6.2',
    'abbr' : '9.6.2',
    'name' : { 'en': 'Dependency relations' },
    'description' : 'Use this domain for words indicating that something is dependent on another thing.',
    'value' : '9.6.2 Dependency relations'
  },
  {
    'guid' : '66dedb31-dd2a-4e94-825e-331590ac59a9',
    'id' : '9.6.2.1',
    'code' : '9.6.2.1',
    'abbr' : '9.6.2.1',
    'name' : { 'en': 'Derivation ' },
    'description' : 'Use this domain for words indicating that something derives from another thing.',
    'value' : '9.6.2.1 Derivation '
  },
  {
    'guid' : '05e20a72-9496-4bba-8097-5605692e83a1',
    'id' : '9.6.2.2',
    'code' : '9.6.2.2',
    'abbr' : '9.6.2.2',
    'name' : { 'en': 'Limitation of topic' },
    'description' : 'Use this domain for words indicating the topic that is being talked about.',
    'value' : '9.6.2.2 Limitation of topic'
  },
  {
    'guid' : '1b4f987d-3eaa-46dd-95ee-e0cb1f30cfbb',
    'id' : '9.6.2.2.1',
    'code' : '9.6.2.2.1',
    'abbr' : '9.6.2.2.1',
    'name' : { 'en': 'In general' },
    'description' : 'Use this domain for words indicating that something is generally true, but not true in every case.',
    'value' : '9.6.2.2.1 In general'
  },
  {
    'guid' : '3759bdda-2b52-43dc-8995-8379e3129dce',
    'id' : '9.6.2.3',
    'code' : '9.6.2.3',
    'abbr' : '9.6.2.3',
    'name' : { 'en': 'Relations involving correspondences' },
    'description' : 'Use this domain for words indicating relations involving correspondences--a situation in which one thing is the same or similar in some respect to something else.',
    'value' : '9.6.2.3 Relations involving correspondences'
  },
  {
    'guid' : '779e4547-dee6-4780-a180-30a740f9574c',
    'id' : '9.6.2.4',
    'code' : '9.6.2.4',
    'abbr' : '9.6.2.4',
    'name' : { 'en': 'Basis' },
    'description' : 'Use this domain for words indicating that something is the basis for another thing.',
    'value' : '9.6.2.4 Basis'
  },
  {
    'guid' : '23bc906d-c15a-4368-b0ca-7443d5e37b83',
    'id' : '9.6.2.5',
    'code' : '9.6.2.5',
    'abbr' : '9.6.2.5',
    'name' : { 'en': 'Cause' },
    'description' : 'Use this domain for words that indicate that someone or something is the cause for an event or state, that one event is the cause for another event or state, or that an event or state is reasonable (having sufficient cause). For instance in the sentence, "John caused David to fall," "John caused" is an enabling proposition that brings about the primary proposition "David fell."',
    'value' : '9.6.2.5 Cause'
  },
  {
    'guid' : 'e173ea34-c216-4702-aa24-ca9ab40d48dd',
    'id' : '9.6.2.5.1',
    'code' : '9.6.2.5.1',
    'abbr' : '9.6.2.5.1',
    'name' : { 'en': 'Reason' },
    'description' : 'Use this domain for words that reason why someone does something.',
    'value' : '9.6.2.5.1 Reason'
  },
  {
    'guid' : 'be3559d9-d69f-4e06-8184-071c35aa2e10',
    'id' : '9.6.2.5.2',
    'code' : '9.6.2.5.2',
    'abbr' : '9.6.2.5.2',
    'name' : { 'en': 'Without cause' },
    'description' : 'Use this domain for words that indicate that an event or state has no cause or reason, or is unreasonable (has insufficient cause).',
    'value' : '9.6.2.5.2 Without cause'
  },
  {
    'guid' : '48d3de9f-3619-4785-b50b-6921ba7eecd6',
    'id' : '9.6.2.6',
    'code' : '9.6.2.6',
    'abbr' : '9.6.2.6',
    'name' : { 'en': 'Result' },
    'description' : 'Use this domain for words indicating that something is the result of another thing.',
    'value' : '9.6.2.6 Result'
  },
  {
    'guid' : '139409c3-7860-4586-897f-85ba3226046c',
    'id' : '9.6.2.6.1',
    'code' : '9.6.2.6.1',
    'abbr' : '9.6.2.6.1',
    'name' : { 'en': 'Without result ' },
    'description' : 'Use this domain for words indicating that something had no result.',
    'value' : '9.6.2.6.1 Without result '
  },
  {
    'guid' : '18bf6c79-6399-4977-be3d-93135302d8c4',
    'id' : '9.6.2.7',
    'code' : '9.6.2.7',
    'abbr' : '9.6.2.7',
    'name' : { 'en': 'Purpose ' },
    'description' : 'Use this domain for words indicating that something was done for the purpose of another thing happening.',
    'value' : '9.6.2.7 Purpose '
  },
  {
    'guid' : '4fdf3cf1-0808-4f11-acdd-9db71550baab',
    'id' : '9.6.2.7.1',
    'code' : '9.6.2.7.1',
    'abbr' : '9.6.2.7.1',
    'name' : { 'en': 'Without purpose ' },
    'description' : 'Use this domain for words indicating that something had no purpose.',
    'value' : '9.6.2.7.1 Without purpose '
  },
  {
    'guid' : 'f858278a-2727-4403-9cf0-565cdececb1e',
    'id' : '9.6.2.8',
    'code' : '9.6.2.8',
    'abbr' : '9.6.2.8',
    'name' : { 'en': 'Condition' },
    'description' : 'Use this section for verbal auxiliaries, affixes, adverbs, and particles that indicate a clause in a conditional sentence (If this is true, then that is true). The following definitions are taken from Bybee, Joan, Revere Perkins, and William Pagliuca. 1994. The evolution of grammar. Chicago and London: University of Chicago Press.',
    'value' : '9.6.2.8 Condition'
  },
  {
    'guid' : '2f28f1ab-476e-4317-8787-124d95d6b9d2',
    'id' : '9.6.2.9',
    'code' : '9.6.2.9',
    'abbr' : '9.6.2.9',
    'name' : { 'en': 'Concession' },
    'description' : 'Use this domain for words indicating that the speaker is conceding a point in a debate.',
    'value' : '9.6.2.9 Concession'
  },
  {
    'guid' : 'f02ae505-d6b7-4b30-9d97-8505d0d1a0c7',
    'id' : '9.6.3',
    'code' : '9.6.3',
    'abbr' : '9.6.3',
    'name' : { 'en': 'Discourse markers' },
    'description' : 'Use this domain for conjunctions and particles that function on the discourse level, and whose meaning and function is uncertain.',
    'value' : '9.6.3 Discourse markers'
  },
  {
    'guid' : '638679c7-1c7c-41da-ab60-0ac7e98fcd72',
    'id' : '9.6.3.1',
    'code' : '9.6.3.1',
    'abbr' : '9.6.3.1',
    'name' : { 'en': 'Markers of transition ' },
    'description' : 'Use this domain for conjunctions that simply move the discourse forward without any specific relationship indicated between what comes before and what comes after.',
    'value' : '9.6.3.1 Markers of transition '
  },
  {
    'guid' : '7c1c3730-3b35-4150-b54e-bf6d344546b3',
    'id' : '9.6.3.2',
    'code' : '9.6.3.2',
    'abbr' : '9.6.3.2',
    'name' : { 'en': 'Markers of emphasis ' },
    'description' : 'Use this domain for words that indicate that the phrase or sentence is particularly important.',
    'value' : '9.6.3.2 Markers of emphasis '
  },
  {
    'guid' : 'c103d339-24f2-45c6-8539-d3c445e15c49',
    'id' : '9.6.3.3',
    'code' : '9.6.3.3',
    'abbr' : '9.6.3.3',
    'name' : { 'en': 'Prompters of attention ' },
    'description' : 'Use this domain for words that are used to get someone"s attention or direct the listener"s attention to something. These may use a verb meaning "look" or "listen". Some may be a word specifically referring to attention. Others may be a greeting. Others may be words that refer to non-verbal communication such as clearing your throat.',
    'value' : '9.6.3.3 Prompters of attention '
  },
  {
    'guid' : '577a9f51-263a-4c80-a439-84ce45b9c7cc',
    'id' : '9.6.3.4',
    'code' : '9.6.3.4',
    'abbr' : '9.6.3.4',
    'name' : { 'en': 'Markers of direct address ' },
    'description' : 'Use this domain for words that the speaker uses to refer to the person he is addressing. These words are usually used when you start talking to someone, but can be used during a speech or conversation to refer to the person you are talking to.',
    'value' : '9.6.3.4 Markers of direct address '
  },
  {
    'guid' : 'fbf40f2e-e743-479d-80b2-63325407d5d1',
    'id' : '9.6.3.5',
    'code' : '9.6.3.5',
    'abbr' : '9.6.3.5',
    'name' : { 'en': 'Markers of identificational and explanatory clauses ' },
    'description' : 'Use this domain for words that begin a clause that identifies a specific case or example of what has just been said, or that explains what has just been said. Specific case: I have just mentioned a general class of things or a general idea and want to give a specific example of what I am talking about. Explanation: I have just said something and I think people might misunderstand, so I want to explain what I mean. Digression: I am talking about a particular topic, but want to say something that does not fit into my topic, so I say something that is about a different topic.',
    'value' : '9.6.3.5 Markers of identificational and explanatory clauses '
  },
  {
    'guid' : '15e0b54b-bb7c-4900-b048-20b718d05f79',
    'id' : '9.6.3.6',
    'code' : '9.6.3.6',
    'abbr' : '9.6.3.6',
    'name' : { 'en': 'Markers of focus' },
    'description' : 'Use this domain for words indicating that one of several things is in focus.',
    'value' : '9.6.3.6 Markers of focus'
  },
  {
    'guid' : '41837400-bdc5-4cbc-a1dc-d793f713f883',
    'id' : '9.6.3.7',
    'code' : '9.6.3.7',
    'abbr' : '9.6.3.7',
    'name' : { 'en': 'Hesitation fillers' },
    'description' : 'Use this domain for words that a speaker uses when he hesitates or pauses while he is speaking in order to think about what he is saying.',
    'value' : '9.6.3.7 Hesitation fillers'
  },
  {
    'guid' : '2e5a80f9-35ae-4850-9627-be530832a781',
    'id' : '9.6.3.8',
    'code' : '9.6.3.8',
    'abbr' : '9.6.3.8',
    'name' : { 'en': 'Honorifics ' },
    'description' : 'Use this domain for words that the speaker uses to show respect or a lack of respect to the person he is addressing. Some languages have elaborate systems of honorifics. Other languages have none. Languages with a stratified social structure often use honorifics. Egalitarian societies generally lack them, but some egalitarian societies may use them. For instance in Nahuatl there are four levels of honorifics. Level 1 is how one addresses intimates, small children, and pets. Level 2 is for strangers and persons treated formally. Level 3 is for respected persons, the dead, and God. Level 4 is for obsequious respect, as for the archbishop in an interview with a priest, and for ritual kin. (Jane H. Hill and Kenneth C. Hill. 1978. Honorific usage in modern Nahuatl: the expression of social distance and respect in the Nahuatl of the Malinche Volcano area, Language 54:123-155.) In Japanese, which has a stratified social structure, a person uses one set of words and affixes when speaking to someone below you in the social hierarchy, such as your wife, children, and pets. A different set of words is used when speaking to peers. Another set is used when speaking to a superior. A fourth set is used when speaking to the emperor. English used to have two pronouns for second person singular. "Thou" was used for equals and inferiors, and "you" was used for superiors. Your language may have special honorific words used as (1) pronouns, (2) affixes, (3) particles, (4) terms of direct address, (5) greetings (6) requests, (7) apologies.',
    'value' : '9.6.3.8 Honorifics '
  },
  {
    'guid' : '5b7666bd-c6c1-45e7-aa2e-1799fcb16d97',
    'id' : '9.7',
    'code' : '9.7',
    'abbr' : '9.7',
    'name' : { 'en': 'Name' },
    'description' : 'Use this domain for general words referring to proper nouns--the name given to a particular person or thing to distinguish it from other things like it. Proper nouns are often not included in a dictionary, or are included in an appendix at the front or back of a dictionary. This is because there are so many of them, they are sometimes difficult to define, and it saves space in the dictionary. For instance place names can be included in a map. So it might be good to type the proper nouns into a special file.',
    'value' : '9.7 Name'
  },
  {
    'guid' : '7b513a02-c3ae-4243-9410-16854d911258',
    'id' : '9.7.1',
    'code' : '9.7.1',
    'abbr' : '9.7.1',
    'name' : { 'en': 'Name of a person' },
    'description' : 'Use this domain for words related to the name of a person. Each culture has a system of personal names to identify individuals and kin groups. The subcategories under this heading should reflect the cultural system. If your language has a special set of names that do not fit any of they domains given here, then set up a special domain.',
    'value' : '9.7.1 Name of a person'
  },
  {
    'guid' : '6b921117-47e9-4717-b3b3-4e170d26b6d9',
    'id' : '9.7.1.1',
    'code' : '9.7.1.1',
    'abbr' : '9.7.1.1',
    'name' : { 'en': 'Personal names' },
    'description' : 'Use this domain for those names that are given to people, that people use to call to each other and to talk about each other.',
    'value' : '9.7.1.1 Personal names'
  },
  {
    'guid' : 'c2ec9cee-7fe3-44f2-9008-c8b42f6f78dd',
    'id' : '9.7.1.2',
    'code' : '9.7.1.2',
    'abbr' : '9.7.1.2',
    'name' : { 'en': 'Family names' },
    'description' : 'Use this domain for the proper names of the families that exist within the language community. If your culture does not use family names, just leave this domain empty.',
    'value' : '9.7.1.2 Family names'
  },
  {
    'guid' : '774cdff1-8cba-4f94-a519-c66abd3b5f49',
    'id' : '9.7.1.3',
    'code' : '9.7.1.3',
    'abbr' : '9.7.1.3',
    'name' : { 'en': 'Clan names' },
    'description' : 'Use this domain for the proper names of the clans that exist within the language community. The distinction between family, clan, tribe, and nation is based on politics and emotion. Our purpose here is not to make political statements, but merely to list the names. There may be no distinction between family and clan, in which case ignore this domain and use the domain "Family names".',
    'value' : '9.7.1.3 Clan names'
  },
  {
    'guid' : '5771f3a1-fda3-4111-9abc-7a0d76c60a79',
    'id' : '9.7.1.4',
    'code' : '9.7.1.4',
    'abbr' : '9.7.1.4',
    'name' : { 'en': 'Tribal names' },
    'description' : 'Use this domain for the proper names of the tribes that exist around the language community, including the name of your own tribe. These tribal names may or may not correspond with the names of countries.',
    'value' : '9.7.1.4 Tribal names'
  },
  {
    'guid' : 'dd3e872a-fb50-4204-9646-7a24c644013b',
    'id' : '9.7.1.5',
    'code' : '9.7.1.5',
    'abbr' : '9.7.1.5',
    'name' : { 'en': 'Names of languages' },
    'description' : 'Use this domain for the proper names of the languages that are spoken in the area around the language community, including the name of your own language. These language names may or may not correspond with the names of countries. Do not try to include every language name in the world, only the neighboring and important ones. For instance you might want to include the languages that border your own and the national language. Give the form that you use. For instance the German people call their language "Deutsch", but in English we call it "German".',
    'value' : '9.7.1.5 Names of languages'
  },
  {
    'guid' : '94f573ff-29f0-42ae-b1d9-f882823b2935',
    'id' : '9.7.1.6',
    'code' : '9.7.1.6',
    'abbr' : '9.7.1.6',
    'name' : { 'en': 'Nickname' },
    'description' : 'Use this domain for common nicknames--an additional name given to a person later in life, often descriptive. Also include general names used to call or refer to someone when you don"t know their name',
    'value' : '9.7.1.6 Nickname'
  },
  {
    'guid' : 'ebb5f3e5-bfe5-4a40-986a-938c1bdb9c76',
    'id' : '9.7.1.7',
    'code' : '9.7.1.7',
    'abbr' : '9.7.1.7',
    'name' : { 'en': 'Terms of endearment' },
    'description' : 'Use this domain for terms of endearment--a name used by lovers or spouses to express love or intimacy. Some languages may have special names used by close friends.',
    'value' : '9.7.1.7 Terms of endearment'
  },
  {
    'guid' : '69541573-f845-4e77-91f6-1e3551fc6c82',
    'id' : '9.7.2',
    'code' : '9.7.2',
    'abbr' : '9.7.2',
    'name' : { 'en': 'Name of a place' },
    'description' : 'Use this domain for words referring to the name of a place.',
    'value' : '9.7.2 Name of a place'
  },
  {
    'guid' : 'ee0585b1-627a-4a71-888d-b5d82619431e',
    'id' : '9.7.2.1',
    'code' : '9.7.2.1',
    'abbr' : '9.7.2.1',
    'name' : { 'en': 'Names of countries' },
    'description' : 'Use this domain for the proper names of the countries that exist around the language community, especially those countries where your language is spoken. Include the name of your own country. Do not list every country in the world, unless your language has developed special names or pronunciations for those countries. Include any country that you refer to in your language, especially those names whose pronunciation you have adapted to fit your language. Give the form of the name that you use, rather than the official spelling. For instance the Japanese refer to their country as "Nihon", but in English will call it "Japan". So\n"Japan" is an English word and should go into an English dictionary. But "Nihon" is not an English word and should not go in the dictionary.',
    'value' : '9.7.2.1 Names of countries'
  },
  {
    'guid' : '85cb1e3c-62ba-4a77-a838-0237707fb0cb',
    'id' : '9.7.2.2',
    'code' : '9.7.2.2',
    'abbr' : '9.7.2.2',
    'name' : { 'en': 'Names of regions' },
    'description' : 'Use this domain for the proper names of the regions within your country or language area. Some of these may be political regions. Others may be informal terms. Give the local pronunciation, rather than some foreign spelling. You may want to limit this domain to just those areas within your language area. However if you have special names for areas outside of your language area, for example "the Mideast", you should include them.',
    'value' : '9.7.2.2 Names of regions'
  },
  {
    'guid' : 'b8e66bb4-140c-45b4-89ce-d9a77b9e5d21',
    'id' : '9.7.2.3',
    'code' : '9.7.2.3',
    'abbr' : '9.7.2.3',
    'name' : { 'en': 'Names of cities' },
    'description' : 'Use this domain for the proper names of cities, towns, and villages in the language area. Include the names of important cities outside of the language area if your language has a special name for the city or a different pronunciation for it. It might be good to use a map for this. In fact it is good to include a map of the language area in a published dictionary. If your language area is very large, there may be hundreds or thousands of cities, towns, and villages. In this case you will have to decide which should be included in the dictionary. Or you could decided to list them in a special section.',
    'value' : '9.7.2.3 Names of cities'
  },
  {
    'guid' : '35a9da32-53ee-44fa-9c65-5a15f88ad283',
    'id' : '9.7.2.4',
    'code' : '9.7.2.4',
    'abbr' : '9.7.2.4',
    'name' : { 'en': 'Names of streets' },
    'description' : 'Use this domain for the proper names of highways, roads, streets, and trails in the language area. If there are many such names, only include the important names (e.g. King"s Highway) or commonly used names (e.g. Main Street).',
    'value' : '9.7.2.4 Names of streets'
  },
  {
    'guid' : '69d366d2-9735-4a1b-b938-7b212932b568',
    'id' : '9.7.2.5',
    'code' : '9.7.2.5',
    'abbr' : '9.7.2.5',
    'name' : { 'en': 'Names of heavenly bodies' },
    'description' : 'Use this domain for the proper names of the heavenly bodies.',
    'value' : '9.7.2.5 Names of heavenly bodies'
  },
  {
    'guid' : '09ac3709-0b0e-4046-b6b2-7869d574aa0d',
    'id' : '9.7.2.6',
    'code' : '9.7.2.6',
    'abbr' : '9.7.2.6',
    'name' : { 'en': 'Names of continents' },
    'description' : 'Use this domain for the proper names of the continents. Only include the names of continents if your language has borrowed or adapted the name and you talk about them in your language.',
    'value' : '9.7.2.6 Names of continents'
  },
  {
    'guid' : 'd90e71bf-2898-4501-9d09-c518999f83e2',
    'id' : '9.7.2.7',
    'code' : '9.7.2.7',
    'abbr' : '9.7.2.7',
    'name' : { 'en': 'Names of mountains' },
    'description' : 'Use this domain for the proper names of the mountains in the language area. Only include the names of mountains outside the language area if your language has borrowed or adapted the name and you talk about them in your language.',
    'value' : '9.7.2.7 Names of mountains'
  },
  {
    'guid' : 'a105e31c-1268-4fc2-8655-838d34860ece',
    'id' : '9.7.2.8',
    'code' : '9.7.2.8',
    'abbr' : '9.7.2.8',
    'name' : { 'en': 'Names of oceans and lakes' },
    'description' : 'Use this domain for the proper names of the oceans and lakes in the language area. Only include the names of oceans and lakes outside the language area if your language has borrowed or adapted the name and you talk about them in your language.',
    'value' : '9.7.2.8 Names of oceans and lakes'
  },
  {
    'guid' : 'e8ec3885-c692-4b90-a5b3-4c86da642666',
    'id' : '9.7.2.9',
    'code' : '9.7.2.9',
    'abbr' : '9.7.2.9',
    'name' : { 'en': 'Names of rivers' },
    'description' : 'Use this domain for the proper names of the rivers in the language area. Only include the names of rivers outside the language area if your language has borrowed or adapted the name and you talk about them in your language.',
    'value' : '9.7.2.9 Names of rivers'
  },
  {
    'guid' : '7d111356-e04e-4891-960c-2f35147eba82',
    'id' : '9.7.3',
    'code' : '9.7.3',
    'abbr' : '9.7.3',
    'name' : { 'en': 'Name of a thing' },
    'description' : 'Use this domain for words related to the name of a thing. Many cultures give names to particular buildings, ships, airplanes, organizations, companies, schools, and other things. If your language has hundreds of names for some kind of thing, it is best to not try to list them all. But if there are a few important names for one kind of thing, set up a domain for them.',
    'value' : '9.7.3 Name of a thing'
  },
  {
    'guid' : '5df53b87-7f59-4f5c-991e-5ae007b68fa9',
    'id' : '9.7.3.1',
    'code' : '9.7.3.1',
    'abbr' : '9.7.3.1',
    'name' : { 'en': 'Names of animals' },
    'description' : 'Use this domain for words referring to the name of an animal. Some cultures give names to domesticated animals or to animals in stories. Think through each kind of domesticated animal.',
    'value' : '9.7.3.1 Names of animals'
  },
  {
    'guid' : '4223d3ba-5560-4c30-b013-4e31fee36329',
    'id' : '9.7.3.2',
    'code' : '9.7.3.2',
    'abbr' : '9.7.3.2',
    'name' : { 'en': 'Names of buildings' },
    'description' : 'Use this domain for words referring to the name of a building.',
    'value' : '9.7.3.2 Names of buildings'
  }
];
