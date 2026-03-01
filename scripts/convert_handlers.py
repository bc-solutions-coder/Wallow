#!/usr/bin/env python3
"""
Convert Wolverine handlers from traditional constructor injection to C# 12 primary constructors.
"""
import re
import sys
from pathlib import Path

def convert_handler(file_path):
    """Convert a single handler file to primary constructor pattern."""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Pattern to match traditional constructor with fields
    pattern = r'(public sealed class \w+Handler)\s*\n\{\s*\n((?:\s*private readonly [^;]+;\s*\n)+)\s*\n\s*public \w+Handler\(([^)]+)\)\s*\n\s*\{([^}]+)\}\s*\n'

    def replacer(match):
        class_decl = match.group(1)
        fields = match.group(2)
        params = match.group(3)
        assignments = match.group(4)

        # Extract parameter names for primary constructor
        param_list = params.strip()

        # Build primary constructor
        new_class_decl = f"{class_decl}({param_list})"

        # Find all field usages and convert _fieldName to fieldName
        field_names = re.findall(r'private readonly \S+ (_\w+);', fields)

        result_content = content
        for field_name in field_names:
            # Convert from _fieldName to fieldName (camelCase)
            param_name = field_name[1:]  # Remove underscore
            # Replace all usages of the field with parameter
            result_content = re.sub(rf'\b{re.escape(field_name)}\b', param_name, result_content)

        # Now do the main replacement
        result_content = re.sub(
            pattern,
            f"{new_class_decl}\\n{{\\n",
            result_content,
            count=1
        )

        return result_content

    # Check if this file needs conversion
    if 'private readonly' not in content:
        return False  # Already converted or no fields

    # Apply conversion
    new_content = content

    # Find class declaration and constructor
    class_match = re.search(
        r'(public sealed class (\w+))\s*\n\{\s*\n((?:\s*(?:private readonly|private static readonly) [^;]+;\s*\n)+)\s*\n\s*public \2\(([^)]*)\)\s*\n\s*\{([^}]*)\}',
        content,
        re.MULTILINE | re.DOTALL
    )

    if not class_match:
        return False  # No traditional constructor found

    class_decl = class_match.group(1)
    class_name = class_match.group(2)
    fields_section = class_match.group(3)
    params = class_match.group(4)
    assignments = class_match.group(5)

    # Check if there are static fields (keep those)
    static_fields = re.findall(r'(\s*private static readonly [^;]+;)\s*\n', fields_section)
    instance_fields = re.findall(r'\s*private readonly \S+ (_\w+);', fields_section)

    if not instance_fields:
        return False  # No instance fields to convert

    # Build new class declaration with primary constructor
    new_class_decl = f"{class_decl}({params.strip()})"

    # Build new content
    new_content = content

    # Replace field names with parameter names throughout the file
    for field_name in instance_fields:
        param_name = field_name[1:]  # Remove underscore
        new_content = re.sub(rf'\b{re.escape(field_name)}\b', param_name, new_content)

    # Remove the old class declaration, fields, and constructor, replace with primary constructor
    if static_fields:
        # Keep static fields
        static_fields_str = ''.join(static_fields)
        replacement = f"{new_class_decl}\\n{{\\n{static_fields_str}\\n"
    else:
        replacement = f"{new_class_decl}\\n{{\\n"

    new_content = re.sub(
        r'(public sealed class \w+)\s*\n\{\s*\n(?:(?:\s*private (?:static )?readonly [^;]+;\s*\n)+)\s*\n\s*public \w+\([^)]*\)\s*\n\s*\{[^}]*\}\s*\n',
        replacement,
        new_content,
        count=1
    )

    # Write back
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(new_content)

    return True

def main():
    # Modules to process
    modules = [
        'Assets', 'Onboarding', 'Comments', 'Compliance',
        'FeatureFlags', 'KnowledgeBase', 'Metering',
        'Reporting', 'Scheduler', 'StatusPage', 'Identity'
    ]

    base_path = Path('/Users/traveler/Repos/Foundry/src/Modules')

    total_converted = 0

    for module in modules:
        module_path = base_path / module
        if not module_path.exists():
            continue

        handler_files = list(module_path.rglob('*Handler.cs'))
        converted_count = 0

        for handler_file in handler_files:
            if 'EventHandler' in handler_file.name:
                # Skip static event handlers
                with open(handler_file, 'r') as f:
                    if 'public static async Task' in f.read():
                        continue

            if convert_handler(handler_file):
                converted_count += 1
                print(f"Converted: {handler_file.relative_to(base_path)}")

        if converted_count > 0:
            print(f"  {module}: {converted_count} handlers converted")
            total_converted += converted_count

    print(f"\\nTotal converted: {total_converted} handlers")

if __name__ == '__main__':
    main()
