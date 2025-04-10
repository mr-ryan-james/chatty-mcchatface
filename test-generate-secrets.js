const { parseSecretsOutput } = require("./generate-docker-secrets")

const mockSecretsOutput = `
OpenAI:ApiKey = sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Jwt:Key = MySuperSecretKeyThatIsLongEnoughForHS256
Jwt:Issuer = MyTestIssuer
Jwt:Audience = MyTestAudience
ConnectionStrings:DefaultConnection = DataSource=test.db
AzureOpenAI:Deployment1:ApiKey = AzureKey1
AzureOpenAI:Deployment1:Endpoint = https://azure1.example.com
AzureOpenAI:Deployment2:ApiKey = AzureKey2
AzureOpenAI:Deployment2:Endpoint = https://azure2.example.com
VertexAI:Location = europe-west1
VertexAI:KeyJsonContent = {
  "type": "service_account",
  "project_id": "test-project-123",
  "private_key_id": "abcdef123456",
  "private_key": "-----BEGIN PRIVATE KEY-----\\nLine1\\nLine2\\n-----END PRIVATE KEY-----\\n",
  "client_email": "test-service-account@test-project-123.iam.gserviceaccount.com",
  "client_id": "12345678901234567890",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/test-service-account%40test-project-123.iam.gserviceaccount.com",
  "universe_domain": "googleapis.com"
}
Anthropic:ApiKey = sk-ant-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

const secrets = parseSecretsOutput(mockSecretsOutput)

console.log(JSON.stringify(secrets, null, 2))

// Simple assertions
if (
    secrets.VertexAI &&
    typeof secrets.VertexAI.KeyJsonContent === "object" // Just check if it was parsed to an object
) {
    console.log("✅ VertexAI.KeyJsonContent parsed as JSON object")
} else {
    console.error("❌ VertexAI.KeyJsonContent was NOT parsed correctly")
}

if (
    secrets.OpenAI &&
    secrets.OpenAI.ApiKey === "sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
) {
    console.log("✅ OpenAI.ApiKey parsed correctly")
} else {
    console.error("❌ OpenAI.ApiKey was NOT parsed correctly")
}

// Add assertion for Jwt:Audience
if (secrets.Jwt && secrets.Jwt.Audience === "MyTestAudience") {
    console.log("✅ Jwt.Audience parsed correctly")
} else {
    console.error("❌ Jwt.Audience was NOT parsed correctly")
}
