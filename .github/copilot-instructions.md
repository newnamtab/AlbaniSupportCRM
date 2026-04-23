# Copilot Instructions

## Project Guidelines
- User has implemented JWT authentication in Blazor WASM following the secure recommendations provided, using HTTP-only cookies instead of localStorage, with automatic token refresh, and proper error handling. The user wants a Claude Agent verification prompt to systematically review the implementation.

---

## Pro Tips

### Tip 1: Share Code in Chunks
If code is long, Claude can handle it. But splitting helps Claude focus.

### Tip 2: Ask for Specific Fixes
If Claude identifies an issue, you can ask:
- "Can you provide the corrected code for AccountService.cs?"
- "Can you create a unit test for the token refresh scenario?"
- "Can you explain why this approach is more secure?"

### Tip 3: Request Code Examples
You can ask Claude:
- "Show me how to test this scenario"
- "What should appsettings.json look like?"
- "Can you create a deployment checklist?"

### Tip 4: Iterative Review
You don't need to share all code at once. You can:
1. Review client-side first
2. Get it working
3. Then review server-side
4. Then review integration

### Tip 5: Before Production
Ask Claude:
- "Is this implementation production-ready?"
- "What security hardening would you recommend?"
- "What performance optimizations are possible?"

---

## What Success Looks Like

After Claude review, you should see:

✅ **Overall Status**: PASS (or PASS WITH IMPROVEMENTS)

✅ **Security Issues**: 0 CRITICAL, 0 HIGH

✅ **Functional Issues**: All scenarios passing

✅ **Code Quality**: No major issues

✅ **Ready to Deploy**: Yes

---

## Troubleshooting

### Claude Can't Find Issues
- Share more detailed code snippets
- Include configuration files
- Ask Claude to trace through a specific flow

### Too Many Issues
- Don't worry - Claude will prioritize
- Fix CRITICAL first, then HIGH, then MEDIUM
- Each fix is explained with code examples

### Confused by Recommendations
- Ask Claude to explain further
- Request code examples showing before/after
- Ask "Why is this better than my current approach?"

---

## Next Steps

1. ✅ You have the verification prompt
2. ✅ You have implementation guidance
3. ⏭️ Go to Claude and start review
4. ⏭️ Share your code files
5. ⏭️ Get verification results
6. ⏭️ Implement recommended fixes
7. ⏭️ Test thoroughly
8. ⏭️ Deploy with confidence!

---

**You're all set!** Your JWT implementation is about to be verified by an expert AI reviewer. 🎉

