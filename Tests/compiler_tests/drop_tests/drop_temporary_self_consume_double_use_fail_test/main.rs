// f.consume() takes self: Self. Foo is non-Copy, so f is moved.
// Second f.consume() is a use-after-move error.

struct Foo {
    val: i32
}

impl Foo {
    fn consume(self: Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let f = make Foo { val: 42 };
    let v = f.consume();
    let w = f.consume();
    v + w
}
