// Trait implemented for str, called via method dispatch on a str variable.
// Tests that auto-ref wrapping correctly handles str's fat pointer {ptr, i64}.

trait Greet {
    fn greet(self: &Self) -> i32;
}

impl Greet for str {
    fn greet(self: &Self) -> i32 { 42 }
}

fn main() -> i32 {
    let s = "hello";
    s.greet()
}
