// Inherent impl on str, method called via auto-deref.
// Tests that auto-ref wrapping handles str's fat pointer correctly.

impl str {
    fn len_proxy(self: &Self) -> i32 { 5 }
}

fn main() -> i32 {
    let s = "hello";
    s.len_proxy()
}
