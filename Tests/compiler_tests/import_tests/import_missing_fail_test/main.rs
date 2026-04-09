struct Foo { val: i32 }

impl Drop for Foo {
    fn Drop(self: &uniq Self) {}
}

fn main() -> i32 { 0 }
