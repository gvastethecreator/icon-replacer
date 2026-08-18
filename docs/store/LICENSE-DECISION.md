# Icon Replacer source-license decision

**Status:** decision required before stable public release  
**Scope:** source repository licensing, not Microsoft Store application licensing

The repository is public, but the current README states that no source-code license has been selected. A public repository without an explicit license generally does not grant the public permission to copy, modify, or redistribute the source.

Microsoft Store publication does not require the source repository to be open source. However, the repository, contribution policy, package metadata, Store listing, and user expectations should communicate one coherent legal posture.

This document does not select a license on behalf of the owner. It records the decision that must be made deliberately.

## Decision options

### Option A — MIT

Characteristics:

- permissive open-source license;
- allows commercial and non-commercial use, modification, and redistribution;
- requires preservation of copyright/license notice;
- no patent grant beyond what may be implied by applicable law;
- aligns with several of the publisher's other public projects.

Use when broad reuse and contribution are desired and patent-language detail is not a priority.

### Option B — Apache License 2.0

Characteristics:

- permissive open-source license;
- explicit patent grant and termination provisions;
- requires notices and marking of changes;
- longer and more operationally detailed than MIT.

Use when explicit patent terms are valuable.

### Option C — source-available custom terms

Characteristics:

- source can remain visible while reuse is limited;
- restrictions can cover commercial redistribution, competing products, hosted services, branding, or other concerns;
- custom terms require careful legal review;
- the project should not describe itself as open source unless the selected terms meet an accepted open-source definition.

Use only after defining the actual product and business goal and obtaining appropriate legal review.

### Option D — all rights reserved / no reuse grant

Characteristics:

- preserves public inspection without granting copy, modification, or redistribution rights;
- should be stated explicitly in a repository license/notice rather than relying only on absence of a license;
- outside contributors need a separate contribution agreement or clear inbound-license policy before their code can be safely accepted.

Use when the repository is public for transparency or portfolio purposes but source reuse is not intended.

## Questions the owner must answer

1. Should third parties be allowed to fork and redistribute Icon Replacer?
2. Should commercial reuse be allowed?
3. Should competing Windows icon tools be allowed to reuse the code?
4. Is an explicit patent grant desired?
5. Should modifications be required to remain open?
6. Is the current contribution model compatible with the chosen inbound license?
7. Are bundled icons, screenshots, fonts, or other assets covered by the same terms as source code?
8. Are third-party dependencies and notices documented separately?
9. Should the Store-distributed binary use the same EULA posture as the source repository?
10. Is the publisher name and copyright holder correctly identified?

## Required repository changes after the decision

- add a root `LICENSE` file containing the selected terms;
- update the README license section;
- update repository metadata and badges;
- update `CONTRIBUTING.md` with the inbound contribution license;
- identify assets that use different terms;
- add or update third-party notices where required;
- review release archives so they contain the correct license/notice;
- ensure the Store listing does not call the project open source unless that is accurate;
- update support and security documentation if the legal entity/publisher name changes.

## Store-specific distinction

These are separate concepts:

```text
Microsoft Store account/publisher identity
Microsoft Store app license/EULA and acquisition rights
source-code repository license
third-party dependency licenses
promotional-artwork and bundled-asset rights
```

Selecting one does not automatically resolve the others.

## Release gate

Before the first stable Microsoft Store publication, record:

- selected option: `[A/B/C/D]`;
- selected license/terms: `[name/version]`;
- copyright holder: `[name/entity]`;
- decision date: `[date]`;
- repository commit implementing it: `[SHA]`;
- asset-license review: `[complete/incomplete]`;
- third-party-notice review: `[complete/incomplete]`;
- legal review, if applicable: `[reference/n-a]`.

Until this decision is complete, the Store submission may be technically testable, but the stable public release should remain on hold.
