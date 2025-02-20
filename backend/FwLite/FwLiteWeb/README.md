## To start up the web server

1. Open a terminal and start FwLiteWeb.exe
1. it should startup on port 49279
1. settings are configured in appsettings.Production.json
1. open a browser and go to http://localhost:49279/swagger, to view the swagger docs

## useful APIs and examples

### get the list of projects

```bash
curl -X GET http://localhost:49279/api/localProjects
```

there should just be one (the project in the `fw-projects` folder).

example:
```json
  {
    "name": "sena-3",
    "crdt": false,
    "fwdata": true,
    "lexbox": false,
    "server": null,
    "id": null,
    "apiEndpoint": "/api/mini-lcm/FwData/sena-3"
  }
```

take the `apiEndpoint` and use it in the next API call

### get entries in a project

once you've selected a project from the previous API you can query it.
```bash
curl -X GET http://localhost:49279{project.apiEndpoint}/entries
```
will return the first 1000 entries sorted by headword. Example response:
```json
[
  {
    "id": "f67e332a-b296-4967-ace3-5583ce66e95b",
    "deletedAt": null,
    "lexemeForm": {
      "seh": "bzwal"
    },
    "citationForm": {
      "seh": "bzwala"
    },
    "literalMeaning": {},
    "senses": [
      {
        "id": "b84dc935-feaf-4489-9434-1da83cfb5e7c",
        "deletedAt": null,
        "entryId": "f67e332a-b296-4967-ace3-5583ce66e95b",
        "definition": {},
        "gloss": {
          "en": "plant",
          "pt": "semear"
        },
        "partOfSpeech": {
          "id": "86ff66f6-0774-407a-a0dc-3eeaf873daf7",
          "name": {
            "en": "Verb",
            "pt": "Verbo"
          },
          "deletedAt": null,
          "predefined": true
        },
        "partOfSpeechId": "86ff66f6-0774-407a-a0dc-3eeaf873daf7",
        "semanticDomains": [],
        "exampleSentences": []
      }
    ],
    "note": {},
    "components": [],
    "complexForms": [],
    "complexFormTypes": []
  },
  {
    "id": "db997605-c9e0-4876-bc61-9bd5ed62c9b1",
    "deletedAt": null,
    "lexemeForm": {
      "seh": "ca"
    },
    "citationForm": {},
    "literalMeaning": {},
    "senses": [
      {
        "id": "5bfee4a9-8bf1-4d74-a3e6-98163225cb42",
        "deletedAt": null,
        "entryId": "db997605-c9e0-4876-bc61-9bd5ed62c9b1",
        "definition": {},
        "gloss": {
          "en": "thing",
          "pt": "coisa"
        },
        "partOfSpeech": null,
        "partOfSpeechId": null,
        "semanticDomains": [],
        "exampleSentences": []
      }
    ],
    "note": {},
    "components": [],
    "complexForms": [],
    "complexFormTypes": []
  },
  {
    "id": "bd7068ce-9d4f-4696-9efc-e66983553492",
    "deletedAt": null,
    "lexemeForm": {
      "seh": "cabanga"
    },
    "citationForm": {},
    "literalMeaning": {},
    "senses": [
      {
        "id": "b84f1059-e013-43da-b6e8-6690c6220dee",
        "deletedAt": null,
        "entryId": "bd7068ce-9d4f-4696-9efc-e66983553492",
        "definition": {
          "en": "corn beer",
          "pt": "cerveja de milho"
        },
        "gloss": {
          "en": "beer",
          "pt": "cerveja"
        },
        "partOfSpeech": {
          "id": "a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5",
          "name": {
            "en": "Noun",
            "pt": "Nome"
          },
          "deletedAt": null,
          "predefined": true
        },
        "partOfSpeechId": "a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5",
        "semanticDomains": [],
        "exampleSentences": [
          {
            "id": "e68bb1cb-a213-484e-8cb8-ff80a7bf0c62",
            "sentence": {
              "seh": "cabanga wadzipa wadidi"
            },
            "translation": {
              "pt": "cerveja forte boa"
            },
            "reference": null,
            "senseId": "b84f1059-e013-43da-b6e8-6690c6220dee",
            "deletedAt": null
          }
        ]
      }
    ],
    "note": {},
    "components": [],
    "complexForms": [],
    "complexFormTypes": []
  }
]
```
if you want to get all the entries, you can use the `?count=-1` query parameter.
