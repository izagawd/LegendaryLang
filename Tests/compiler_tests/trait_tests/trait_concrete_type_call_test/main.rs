trait Default: Sized {
    fn default() -> Self;
}

impl Default for i32 {
    fn default() -> i32 {
        99
    }
}

fn main() -> i32 {
    i32.default()
}
