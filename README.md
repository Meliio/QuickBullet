# QuickBullet

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

input
input.user
input.pass
input.username
input.password
data.headers
data.cookies
data.proxy

BROWSERACTION Open
BROWSERACTION Close
BROWSERACTION GetCookies
BROWSERACTION SetCookies
BROWSERACTION Clearcookies

PAGEACTION Click ""
PAGEACTION Delay ""
PAGEACTION Evaluate ""
PAGEACTION Goto ""
PAGEACTION PressKey ""
PAGEACTION GetContent
PAGEACTION Reload
PAGEACTION SendKey "" "" DELAY ""
PAGEACTION SetHeaders
  HEADER "name: value"
PAGEACTION WaitForResponse ""
PAGEACTION WaitForSelector ""
PAGEACTION WaitForTimeout ""

FUNCTION Base64Decode "" -> VAR ""
FUNCTION Base64Encode "" -> VAR ""
FUNCTION ClearCookies
FUNCTION Constant "" -> VAR ""
FUNCTION CurrentUnixTime -> VAR ""
FUNCTION Hash SHA512 "" -> VAR ""
FUNCTION GetRandomUA -> VAR ""
FUNCTION HtmlDecode ""-> VAR ""
FUNCTION HtmlEncode ""-> VAR ""
FUNCTION UrlDecode ""-> VAR ""
FUNCTION UrlEncode ""-> VAR ""
FUNCTION Length ""-> VAR ""
FUNCTION Replace ""-> VAR ""
FUNCTION ToLowercase ""-> VAR ""
FUNCTION ToUppercase "" -> VAR ""

SET VAR "varName" "value"
SET CAP "capName" "value"
SET USEPROXY FALSE