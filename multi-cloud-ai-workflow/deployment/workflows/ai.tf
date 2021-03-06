# #################################
# #  Step Functions : Lambdas for ai Workflow
# #################################

resource "aws_lambda_function" "ai-01-validate-workflow-input" {
  filename         = "./../workflows/ai/01-ValidateWorkflowInput/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-01-validate-workflow-input")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.ValidateWorkflowInput::Mcma.Aws.Workflows.Ai.ValidateWorkflowInput.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/01-ValidateWorkflowInput/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
    }
  }
}

resource "aws_lambda_function" "ai-02-extract-speech-to-text" {
  filename         = "./../workflows/ai/02-ExtractSpeechToText/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-02-extract-speech-to-text")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.ExtractSpeechToText::Mcma.Aws.Workflows.Ai.ExtractSpeechToText.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/02-ExtractSpeechToText/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
      ACTIVITY_CALLBACK_URL = "${local.workflow_activity_callback_handler_url}"
      ACTIVITY_ARN          = "${aws_sfn_activity.ai-02-extract-speech-to-text.id}"
    }
  }
}

resource "aws_sfn_activity" "ai-02-extract-speech-to-text" {
  name = "${var.global_prefix}-ai-02-extract-speech-to-text"
}

resource "aws_lambda_function" "ai-03-register-speech-to-text-output" {
  filename         = "./../workflows/ai/03-RegisterSpeechToTextOutput/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-03-register-speech-to-text-output")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.RegisterSpeechToTextOutput::Mcma.Aws.Workflows.Ai.RegisterSpeechToTextOutput.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/03-RegisterSpeechToTextOutput/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
    }
  }
}

resource "aws_lambda_function" "ai-04-translate-speech-transcription" {
  filename         = "./../workflows/ai/04-TranslateSpeechTranscription/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-04-translate-speech-transcription")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.TranslateSpeechTranscription::Mcma.Aws.Workflows.Ai.TranslateSpeechTranscription.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/04-TranslateSpeechTranscription/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
      ACTIVITY_CALLBACK_URL = "${local.workflow_activity_callback_handler_url}"
      ACTIVITY_ARN          = "${aws_sfn_activity.ai-04-translate-speech-transcription.id}"
    }
  }
}

resource "aws_sfn_activity" "ai-04-translate-speech-transcription" {
  name = "${var.global_prefix}-ai-04-translate-speech-transcription"
}

resource "aws_lambda_function" "ai-05-register-speech-translation" {
  filename         = "./../workflows/ai/05-RegisterSpeechTranslation/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-05-register-speech-translation")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.RegisterSpeechTranslation::Mcma.Aws.Workflows.Ai.RegisterSpeechTranslation.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/05-RegisterSpeechTranslation/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
    }
  }
}

resource "aws_lambda_function" "ai-06-detect-celebrities-aws" {
  filename         = "./../workflows/ai/06-DetectCelebritiesAws/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-06-detect-celebrities-aws")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.DetectCelebritiesAws::Mcma.Aws.Workflows.Ai.DetectCelebritiesAws.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/06-DetectCelebritiesAws/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
      ACTIVITY_CALLBACK_URL = "${local.workflow_activity_callback_handler_url}"
      ACTIVITY_ARN          = "${aws_sfn_activity.ai-06-detect-celebrities-aws.id}"
    }
  }
}

resource "aws_sfn_activity" "ai-06-detect-celebrities-aws" {
  name = "${var.global_prefix}-ai-06-detect-celebrities-aws"
}

resource "aws_lambda_function" "ai-08-detect-celebrities-azure" {
  filename         = "./../workflows/ai/08-DetectCelebritiesAzure/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-08-detect-celebrities-azure")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.DetectCelebritiesAzure::Mcma.Aws.Workflows.Ai.DetectCelebritiesAzure.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/08-DetectCelebritiesAzure/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
      ACTIVITY_CALLBACK_URL = "${local.workflow_activity_callback_handler_url}"
      ACTIVITY_ARN          = "${aws_sfn_activity.ai-08-detect-celebrities-azure.id}"
    }
  }
}

resource "aws_sfn_activity" "ai-08-detect-celebrities-azure" {
  name = "${var.global_prefix}-ai-08-detect-celebrities-azure"
}

resource "aws_lambda_function" "ai-07-register-celebrities-info-aws" {
  filename         = "./../workflows/ai/07-RegisterCelebritiesInfoAws/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-07-register-celebrities-info-aws")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.RegisterCelebritiesInfoAws::Mcma.Aws.Workflows.Ai.RegisterCelebritiesInfoAws.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/07-RegisterCelebritiesInfoAws/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
    }
  }
}

resource "aws_lambda_function" "ai-09-register-celebrities-info-azure" {
  filename         = "./../workflows/ai/09-RegisterCelebritiesInfoAzure/dist/lambda.zip"
  function_name    = "${format("%.64s", "${var.global_prefix}-ai-09-register-celebrities-info-azure")}"
  role             = "${aws_iam_role.iam_for_exec_lambda.arn}"
  handler          = "Mcma.Aws.Workflows.Ai.RegisterCelebritiesInfoAzure::Mcma.Aws.Workflows.Ai.RegisterCelebritiesInfoAzure.Function::Handler"
  source_code_hash = "${base64sha256(file("./../workflows/ai/09-RegisterCelebritiesInfoAzure/dist/lambda.zip"))}"
  runtime          = "dotnetcore2.1"
  timeout          = "60"
  memory_size      = "256"

  environment {
    variables = {
      SERVICES_URL          = "${var.services_url}"
      SERVICES_AUTH_TYPE    = "${var.services_auth_type}"
      SERVICES_AUTH_CONTEXT = "${var.services_auth_context}"
      REPOSITORY_BUCKET     = "${var.repository_bucket}"
      TEMP_BUCKET           = "${var.temp_bucket}"
      WEBSITE_BUCKET        = "${var.website_bucket}"
    }
  }
}

# #################################
# #  Step Functions : AI Workflow
# #################################

data "template_file" "ai-workflow" {
  template = "${file("workflows/ai.json")}"

  vars {
    lambda-01-validate-workflow-input          = "${aws_lambda_function.ai-01-validate-workflow-input.arn}"
    lambda-02-extract-speech-to-text           = "${aws_lambda_function.ai-02-extract-speech-to-text.arn}"
    activity-02-extract-speech-to-text         = "${aws_sfn_activity.ai-02-extract-speech-to-text.id}"
    lambda-03-register-speech-to-text-output   = "${aws_lambda_function.ai-03-register-speech-to-text-output.arn}"
    lambda-04-translate-speech-transcription   = "${aws_lambda_function.ai-04-translate-speech-transcription.arn}"
    activity-04-translate-speech-transcription = "${aws_sfn_activity.ai-04-translate-speech-transcription.id}"
    lambda-05-register-speech-translation      = "${aws_lambda_function.ai-05-register-speech-translation.arn}"
    lambda-06-detect-celebrities-aws           = "${aws_lambda_function.ai-06-detect-celebrities-aws.arn}"
    activity-06-detect-celebrities-aws         = "${aws_sfn_activity.ai-06-detect-celebrities-aws.id}"
    lambda-08-detect-celebrities-azure         = "${aws_lambda_function.ai-08-detect-celebrities-azure.arn}"
    activity-08-detect-celebrities-azure       = "${aws_sfn_activity.ai-08-detect-celebrities-azure.id}"
    lambda-07-register-celebrities-info-aws    = "${aws_lambda_function.ai-07-register-celebrities-info-aws.arn}"
    lambda-09-register-celebrities-info-azure  = "${aws_lambda_function.ai-09-register-celebrities-info-azure.arn}"
    lambda-process-workflow-completion         = "${aws_lambda_function.process-workflow-completion.arn}"
    lambda-process-workflow-failure            = "${aws_lambda_function.process-workflow-failure.arn}"
  }
}

resource "aws_sfn_state_machine" "ai-workflow" {
  name       = "${var.global_prefix}-ai-workflow"
  role_arn   = "${aws_iam_role.iam_for_state_machine_execution.arn}"
  definition = "${data.template_file.ai-workflow.rendered}"
}

output "ai_workflow_id" {
  value = "${aws_sfn_state_machine.ai-workflow.id}"
}
