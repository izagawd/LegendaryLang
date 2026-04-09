// make Foo { val: 42 }.consume() where consume takes self: Self.
// The temporary is moved into the method. Method returns self.val.
// No spill needed — the method owns the value. Result: 42.

struct Foo {
    val: i32
}

impl Foo {
    fn consume(self: Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    make Foo { val: 42 }.consume()
}
