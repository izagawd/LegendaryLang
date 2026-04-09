// foo.get_bar_ref() returns &Bar. Then r.consume() takes self: Self (Bar).
// Bar is non-Copy. You cannot move a Bar out of a &Bar reference.

struct Bar {
    val: i32
}

impl Bar {
    fn consume(self: Self) -> i32 {
        self.val
    }
}

struct Foo {
    bar: Bar
}

impl Foo {
    fn get_bar_ref(self: &Self) -> &Bar {
        &self.bar
    }
}

fn main() -> i32 {
    let foo = make Foo { bar: make Bar { val: 42 } };
    let r: &Bar = foo.get_bar_ref();
    r.consume()
}
