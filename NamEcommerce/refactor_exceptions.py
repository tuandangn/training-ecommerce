import os
import re

def refactor_exceptions(directory):
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs') and 'Exception' in file and file != 'NamEcommerceDomainException.cs':
                filepath = os.path.join(root, file)
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Check if it's a primary exception definition using primary constructor
                # e.g., public sealed class OrderIsNotFoundException(Guid id) : Exception($"Order with id '{id}' is not found");
                
                # We need to extract the class name
                class_match = re.search(r'class\s+([A-Za-z0-9_]+Exception)', content)
                if not class_match:
                    continue
                class_name = class_match.group(1)
                
                error_code = f'"Error.{class_name}"'
                
                # Replace : Exception(...) with : NamEcommerceDomainException(error_code, ...)
                # First handle primary constructors: public sealed class X(args) : Exception(...)
                # We will just replace : Exception( with : NamEcommerceDomainException("Error.ClassName", 
                # But wait, what if it's ` : Exception($"Order with id '{id}' is not found");`
                # It's better to just regex replace the Exception call.
                
                # Pattern to match: : Exception(anything)
                # But we want to preserve parameters if we can, or just pass the parameters from the primary constructor.
                # Actually, let's just find the primary constructor arguments.
                
                # Regex for primary constructor parameters: class ClassName(type1 arg1, type2 arg2)
                param_match = re.search(fr'class\s+{class_name}\((.*?)\)', content)
                args = []
                if param_match and param_match.group(1).strip():
                    params_str = param_match.group(1)
                    # split by comma
                    for param in params_str.split(','):
                        parts = param.strip().split(' ')
                        if len(parts) >= 2:
                            # last part is arg name
                            arg_name = parts[-1]
                            args.append(arg_name)
                
                # Now build the new base call
                base_call = f' : NamEcommerceDomainException({error_code}'
                if args:
                    base_call += ', ' + ', '.join(args)
                base_call += ')'
                
                # Replace the entire : Exception(...) part
                new_content = re.sub(r':\s*Exception\s*\(.*?\)', base_call, content, flags=re.DOTALL)
                
                # Also handle normal constructors if they exist
                # public X(type arg) : base(...)
                # We can replace : base(...) with : base("Error.X", arg)
                # This is a bit more complex, let's see how many there are.
                
                # Write back
                if new_content != content:
                    with open(filepath, 'w', encoding='utf-8') as f:
                        f.write(new_content)
                    print(f'Refactored {file}')

if __name__ == '__main__':
    refactor_exceptions(r'd:\Learning\NamTraining\training-ecommerce\NamEcommerce\Domain\NamEcommerce.Domain.Shared\Exceptions')
