# QuickBullet

```
[SETTINGS]
{
   "name":"example.com",
   "AdditionalInfo":"",
   "customInputs":[
      {
         "Description":"",
         "Name":""
      }
   ],
   "inputRules":[
      {
         "name":"input.PASS",
         "regex":""
      }
   ]
}
[SCRIPT]

REQUEST GET "http://example.com"
```
input<br />
input.user<br />
input.pass<br />
input.username<br />
input.password<br />
data.headers<br />
data.cookies<br />
data.proxy<br />

BROWSERACTION Open<br />
BROWSERACTION Close<br />
BROWSERACTION GetCookies<br />
BROWSERACTION SetCookies<br />
BROWSERACTION Clearcookies<br />

PAGEACTION Click ""<br />
PAGEACTION Delay ""<br />
PAGEACTION Evaluate ""<br />
PAGEACTION Goto ""<br />
PAGEACTION PressKey ""<br />
PAGEACTION GetContent<br />
PAGEACTION Reload<br />
PAGEACTION SendKey "" "" DELAY ""<br />
PAGEACTION SetHeaders<br />
  HEADER "name: value"<br />
PAGEACTION WaitForResponse ""<br />
PAGEACTION WaitForSelector ""<br />
PAGEACTION WaitForTimeout ""<br />

FUNCTION Base64Decode "" -> VAR ""<br />
FUNCTION Base64Encode "" -> VAR ""<br />
FUNCTION ClearCookies<br />
FUNCTION Constant "" -> VAR ""<br />
FUNCTION CurrentUnixTime -> VAR ""<br />
FUNCTION Hash SHA512 "" -> VAR ""<br />
FUNCTION GetRandomUA -> VAR ""<br />
FUNCTION HtmlDecode ""-> VAR ""<br />
FUNCTION HtmlEncode ""-> VAR ""<br />
FUNCTION UrlDecode ""-> VAR ""<br />
FUNCTION UrlEncode ""-> VAR ""<br />
FUNCTION Length ""-> VAR ""<br />
FUNCTION Replace ""-> VAR ""<br />
FUNCTION ToLowercase ""-> VAR ""<br />
FUNCTION ToUppercase "" -> VAR ""<br />

SET VAR "varName" "value"<br />
SET CAP "capName" "value"<br />
SET USEPROXY FALSE<br />
