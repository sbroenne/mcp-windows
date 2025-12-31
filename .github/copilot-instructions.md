# Copilot Instructions for mcp-windows

## LLM Test Scenarios - Critical Rules

**NEVER modify test scenario USER prompts to include implementation hints.**

Test scenarios represent what REAL USERS would say. If a test fails because the LLM can't figure something out:
1. ✅ Improve the TOOL GUIDANCE (descriptions, parameter hints)
2. ✅ Improve the SYSTEM PROMPTS (WindowsAutomationPrompts.cs)
3. ❌ NEVER add hints to the test USER prompts (this defeats the purpose of the test)

The test USER prompts should be:
- Natural language a real user would type
- Free of implementation details (tool names, parameter names, exact syntax)
- The "specification" of what the LLM should be able to handle

If a test fails, ASK THE USER before making changes - don't assume modifying test prompts is acceptable.

## Constitution Principle VI

Tools MUST be "dumb actuators"—return raw data for LLM interpretation. This means:
- No `app` parameter that does automatic window lookup
- Use explicit handles obtained from `find` or `list` actions
- The LLM is responsible for orchestration, tools just execute
