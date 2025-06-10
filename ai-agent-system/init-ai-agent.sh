#!/bin/bash

# AI Agent Prompt System Initializer
# Version: 1.0.0
# Supports: Claude Code, Cline, Cursor, GitHub Copilot

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Box drawing characters - Using basic ASCII for compatibility
BOX_H="="
BOX_V="|"
BOX_TL="+"
BOX_TR="+"
BOX_BL="+"
BOX_BR="+"
LIGHT_H="-"
LIGHT_V="|"
LIGHT_TL="+"
LIGHT_TR="+"
LIGHT_BL="+"
LIGHT_BR="+"

# Script configuration - Script is in ai-agent-system directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CORE_PROMPT="$SCRIPT_DIR/core-prompt.md"
PROJECT_TEMPLATE="$SCRIPT_DIR/project-template.md"
GUIDES_DIR="$SCRIPT_DIR/guides"

# Flags
FORCE_OVERWRITE=false
WITH_PROJECT=false
GUIDES=""
TOOL=""

# Function to print a header box
print_header() {
    local text="$1"
    local width=50
    local padding=$(( (width - ${#text} - 2) / 2 ))
    
    echo -e "${CYAN}"
    echo -e "${BOX_TL}$(printf "%${width}s" | tr ' ' "$BOX_H")${BOX_TR}"
    printf "${BOX_V}%*s%s%*s${BOX_V}\n" $padding "" "$text" $((width - ${#text} - padding)) ""
    echo -e "${BOX_BL}$(printf "%${width}s" | tr ' ' "$BOX_H")${BOX_BR}"
    echo -e "${NC}"
}

# Function to print a section box
print_section() {
    local text="$1"
    local width=40
    
    echo -e "${BLUE}"
    echo -e "${LIGHT_TL}$(printf "%${width}s" | tr ' ' "$LIGHT_H")${LIGHT_TR}"
    printf "${LIGHT_V} %-$((width-2))s ${LIGHT_V}\n" "$text"
    echo -e "${LIGHT_BL}$(printf "%${width}s" | tr ' ' "$LIGHT_H")${LIGHT_BR}"
    echo -e "${NC}"
}

# Function to print status messages
print_status() {
    local type="$1"
    local message="$2"
    
    case "$type" in
        "info")
            echo -e "${BLUE}[?]${NC} $message"
            ;;
        "success")
            echo -e "${GREEN}[?]${NC} $message"
            ;;
        "warning")
            echo -e "${YELLOW}[!]${NC} $message"
            ;;
        "error")
            echo -e "${RED}[?]${NC} $message"
            ;;
        "working")
            echo -e "${PURPLE}[?]${NC} $message"
            ;;
    esac
}

# Function to print usage
show_usage() {
    print_header "AI Agent Prompt System Initializer"
    
    echo -e "${WHITE}USAGE:${NC}"
    echo -e "  ./init-ai-agent.sh <tool> [options]"
    echo ""
    
    echo -e "${WHITE}TOOLS (in priority order):${NC}"
    echo -e "  ${GREEN}claude${NC}   - Claude Code (creates CLAUDE.md)"
    echo -e "  ${GREEN}cline${NC}    - Cline (creates .clinerules/ directory)"
    echo -e "  ${GREEN}cursor${NC}   - Cursor (creates .cursor/rules/ directory)"
    echo -e "  ${GREEN}copilot${NC}  - GitHub Copilot (creates .github/copilot-instructions.md)"
    echo ""
    
    echo -e "${WHITE}OPTIONS:${NC}"
    echo -e "  ${CYAN}--force${NC}              Overwrite existing files without prompting"
    echo -e "  ${CYAN}--with-project${NC}       Include project instructions if found"
    echo -e "  ${CYAN}--guides <list>${NC}      Include specialized guides (performance,testing,documentation)"
    echo -e "  ${CYAN}--help${NC}               Show this help message"
    echo ""
    
    echo -e "${WHITE}EXAMPLES:${NC}"
    echo -e "  ${CYAN}./init-ai-agent.sh claude${NC}"
    echo -e "  ${CYAN}./init-ai-agent.sh cline --with-project${NC}"
    echo -e "  ${CYAN}./init-ai-agent.sh cursor --guides performance,testing${NC}"
    echo -e "  ${CYAN}./init-ai-agent.sh copilot --force --with-project${NC}"
    echo ""
    
    echo -e "${YELLOW}Note: Files will be created in the current directory${NC}"
    echo -e "${BLUE}Tip: Run this from your project directory, not from ai-agent-system${NC}"
}

# Function to validate arguments
validate_args() {
    if [[ -z "$TOOL" ]]; then
        print_status "error" "No tool specified"
        echo ""
        show_usage
        exit 1
    fi
    
    case "$TOOL" in
        "claude"|"cline"|"cursor"|"copilot")
            # Valid tools
            ;;
        *)
            print_status "error" "Invalid tool: $TOOL"
            echo -e "${YELLOW}Valid tools: claude, cline, cursor, copilot${NC}"
            exit 1
            ;;
    esac
}

# Function to check prerequisites
check_prerequisites() {
    print_section "Checking Prerequisites"
    
    # Check if core prompt exists
    if [[ ! -f "$CORE_PROMPT" ]]; then
        print_status "error" "Core prompt not found: $CORE_PROMPT"
        print_status "info" "Make sure the ai-agent-system directory is complete"
        exit 1
    fi
    print_status "success" "Core prompt found"
    
    # Check for project instructions if requested
    if [[ "$WITH_PROJECT" == true ]]; then
        local project_files=(project-instructions-*.md)
        if [[ -f "${project_files[0]}" ]]; then
            print_status "success" "Project instructions found: ${project_files[0]}"
        else
            print_status "warning" "No project instructions found (project-instructions-*.md)"
            print_status "info" "Continuing without project context"
            WITH_PROJECT=false
        fi
    fi
    
    # Check for guides if requested
    if [[ -n "$GUIDES" ]]; then
        IFS=',' read -ra GUIDE_LIST <<< "$GUIDES"
        for guide in "${GUIDE_LIST[@]}"; do
            local guide_file="$GUIDES_DIR/${guide}-guide.md"
            if [[ -f "$guide_file" ]]; then
                print_status "success" "Guide found: $guide"
            else
                print_status "warning" "Guide not found: $guide"
            fi
        done
    fi
    
    echo ""
}

# Function to detect existing tool files
detect_existing_files() {
    print_section "Detecting Existing Configuration"
    
    local found_files=()
    
    # Check for various tool files
    [[ -f "CLAUDE.md" ]] && found_files+=("CLAUDE.md (Claude Code)")
    [[ -f ".clinerules" ]] && found_files+=(".clinerules (Cline - simple)")
    [[ -d ".clinerules" ]] && found_files+=(".clinerules/ (Cline - modular)")
    [[ -f ".cursorrules" ]] && found_files+=(".cursorrules (Cursor - legacy)")
    [[ -d ".cursor" ]] && found_files+=(".cursor/ (Cursor - modern)")
    [[ -f ".github/copilot-instructions.md" ]] && found_files+=(".github/copilot-instructions.md (Copilot)")
    
    if [[ ${#found_files[@]} -eq 0 ]]; then
        print_status "info" "No existing AI tool configurations found"
    else
        print_status "warning" "Found existing configurations:"
        for file in "${found_files[@]}"; do
            echo -e "    ${YELLOW}?${NC} $file"
        done
    fi
    
    echo ""
}

# Main function to create tool configuration
create_tool_config() {
    print_section "Creating $TOOL Configuration"
    
    case "$TOOL" in
        "claude")
            create_claude_config
            ;;
        "cline")
            create_cline_config
            ;;
        "cursor")
            create_cursor_config
            ;;
        "copilot")
            create_copilot_config
            ;;
    esac
}

# Function to prompt user for overwrite confirmation
prompt_overwrite() {
    local file="$1"
    
    if [[ "$FORCE_OVERWRITE" == true ]]; then
        return 0  # Force overwrite, don't prompt
    fi
    
    echo -e "${YELLOW}[?]${NC} File ${WHITE}$file${NC} already exists."
    echo -e "    ${CYAN}o${NC} - Overwrite"
    echo -e "    ${CYAN}b${NC} - Backup and overwrite"
    echo -e "    ${CYAN}s${NC} - Skip (keep existing)"
    echo -n "    Choose action [o/b/s]: "
    
    read -r choice
    case "$choice" in
        o|O)
            return 0  # Overwrite
            ;;
        b|B)
            local backup="${file}.backup.$(date +%Y%m%d_%H%M%S)"
            cp "$file" "$backup" 2>/dev/null || cp -r "$file" "$backup"
            print_status "info" "Backed up to: $backup"
            return 0  # Overwrite after backup
            ;;
        s|S|*)
            return 1  # Skip
            ;;
    esac
}

# Function to build content from core prompt and optional additions
build_content() {
    local content=""
    
    # Always include core prompt
    if [[ -f "$CORE_PROMPT" ]]; then
        content+="$(cat "$CORE_PROMPT")"
        content+="\n\n"
    fi
    
    # Add project instructions if requested and found
    if [[ "$WITH_PROJECT" == true ]]; then
        local project_files=(project-instructions-*.md)
        if [[ -f "${project_files[0]}" ]]; then
            content+="---\n\n"
            content+="# Project-Specific Instructions\n\n"
            content+="$(cat "${project_files[0]}")"
            content+="\n\n"
        fi
    fi
    
    # Add specialized guides if requested
    if [[ -n "$GUIDES" ]]; then
        content+="---\n\n"
        content+="# Specialized Guides\n\n"
        
        IFS=',' read -ra GUIDE_LIST <<< "$GUIDES"
        for guide in "${GUIDE_LIST[@]}"; do
            local guide_file="$GUIDES_DIR/${guide}-guide.md"
            if [[ -f "$guide_file" ]]; then
                content+="## ${guide^} Guide\n\n"
                content+="$(cat "$guide_file")"
                content+="\n\n"
            fi
        done
    fi
    
    echo "$content"
}

# Claude Code configuration creation
create_claude_config() {
    print_status "working" "Creating Claude Code configuration..."
    
    local target_file="CLAUDE.md"
    
    # Check if file exists and handle overwrite logic
    if [[ -f "$target_file" ]]; then
        if ! prompt_overwrite "$target_file"; then
            print_status "info" "Skipped: $target_file already exists"
            return 0
        fi
    fi
    
    # Build the content
    local content
    content=$(build_content)
    
    # Create the CLAUDE.md file
    cat > "$target_file" << EOF
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

$content
EOF

    print_status "success" "Created: $target_file"
    
    # Show what was included
    echo -e "    ${CYAN}?${NC} Core coding standards and workflow protocols"
    
    if [[ "$WITH_PROJECT" == true ]]; then
        local project_files=(project-instructions-*.md)
        if [[ -f "${project_files[0]}" ]]; then
            echo -e "    ${CYAN}?${NC} Project-specific instructions: ${project_files[0]}"
        fi
    fi
    
    if [[ -n "$GUIDES" ]]; then
        IFS=',' read -ra GUIDE_LIST <<< "$GUIDES"
        for guide in "${GUIDE_LIST[@]}"; do
            local guide_file="$GUIDES_DIR/${guide}-guide.md"
            if [[ -f "$guide_file" ]]; then
                echo -e "    ${CYAN}?${NC} ${guide^} guide"
            fi
        done
    fi
    
    echo ""
    print_status "info" "Claude Code will automatically load this context on startup"
}

# Cline configuration creation (modular .clinerules/ directory)
create_cline_config() {
    print_status "working" "Creating Cline modular configuration..."
    
    local target_dir=".clinerules"
    
    # Check if directory exists
    if [[ -d "$target_dir" ]]; then
        if ! prompt_overwrite "$target_dir (directory)"; then
            print_status "info" "Skipped: $target_dir directory already exists"
            return 0
        else
            # Remove existing directory
            rm -rf "$target_dir"
        fi
    fi
    
    # Check if single .clinerules file exists
    if [[ -f ".clinerules" ]]; then
        if ! prompt_overwrite ".clinerules (will be replaced with directory)"; then
            print_status "info" "Skipped: .clinerules file already exists"
            return 0
        else
            rm -f ".clinerules"
        fi
    fi
    
    # Create the .clinerules directory
    mkdir -p "$target_dir"
    
    # Create core standards file
    local content
    content=$(cat "$CORE_PROMPT")
    cat > "$target_dir/01-core-standards.md" << EOF
# Core Development Standards

$content
EOF
    
    print_status "success" "Created: $target_dir/01-core-standards.md"
    echo -e "    ${CYAN}?${NC} Core coding standards and workflow protocols"
    
    # Add project instructions if requested
    if [[ "$WITH_PROJECT" == true ]]; then
        local project_files=(project-instructions-*.md)
        if [[ -f "${project_files[0]}" ]]; then
            cat > "$target_dir/02-project-context.md" << EOF
# Project-Specific Context

$(cat "${project_files[0]}")
EOF
            print_status "success" "Created: $target_dir/02-project-context.md"
            echo -e "    ${CYAN}?${NC} Project-specific instructions: ${project_files[0]}"
        fi
    fi
    
    # Add specialized guides if requested
    if [[ -n "$GUIDES" ]]; then
        local guide_content="# Specialized Development Guides\n\n"
        local guides_added=false
        
        IFS=',' read -ra GUIDE_LIST <<< "$GUIDES"
        for guide in "${GUIDE_LIST[@]}"; do
            local guide_file="$GUIDES_DIR/${guide}-guide.md"
            if [[ -f "$guide_file" ]]; then
                guide_content+="## ${guide^} Guide\n\n"
                guide_content+="$(cat "$guide_file")\n\n"
                guides_added=true
                echo -e "    ${CYAN}?${NC} ${guide^} guide"
            fi
        done
        
        if [[ "$guides_added" == true ]]; then
            echo -e "$guide_content" > "$target_dir/03-specialized-guides.md"
            print_status "success" "Created: $target_dir/03-specialized-guides.md"
        fi
    fi
    
    echo ""
    print_status "info" "Cline will automatically load all rules from .clinerules/ directory"
    print_status "info" "Use Cline v3.13+ to toggle individual rule files on/off"
}

# Cursor configuration creation (Task 4)
create_cursor_config() {
    print_status "working" "Creating modern Cursor configuration..."
    print_status "info" "Modern .cursor/rules/ directory creation - TASK 4"
    print_status "warning" "Implementation pending - use Claude or Cline for now"
}

# GitHub Copilot configuration creation (Task 5)
create_copilot_config() {
    print_status "working" "Creating GitHub Copilot configuration..."
    print_status "info" ".github/copilot-instructions.md creation - TASK 5"
    print_status "warning" "Implementation pending - use Claude or Cline for now"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --force)
            FORCE_OVERWRITE=true
            shift
            ;;
        --with-project)
            WITH_PROJECT=true
            shift
            ;;
        --guides)
            GUIDES="$2"
            shift 2
            ;;
        --help)
            show_usage
            exit 0
            ;;
        -*)
            print_status "error" "Unknown option: $1"
            show_usage
            exit 1
            ;;
        *)
            if [[ -z "$TOOL" ]]; then
                TOOL="$1"
            else
                print_status "error" "Multiple tools specified: $TOOL and $1"
                exit 1
            fi
            shift
            ;;
    esac
done

# Main execution flow
main() {
    clear
    print_header "AI Agent Prompt System Initializer"
    
    validate_args
    check_prerequisites
    detect_existing_files
    create_tool_config
    
    echo ""
    print_status "success" "Configuration setup complete!"
    print_status "info" "Ready to start coding with enhanced AI assistance"
    
    echo ""
    print_section "Next Steps"
    echo -e "${WHITE}1.${NC} Open your project in your chosen tool"
    echo -e "${WHITE}2.${NC} Start a new chat/session"
    echo -e "${WHITE}3.${NC} Use activation prompt: ${CYAN}REMEMBER${NC}"
    echo -e "${WHITE}4.${NC} Begin enhanced AI-assisted development!"
    echo ""
}

# Run main function
main "$@"
