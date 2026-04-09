// make Foo { val: 42 }.into_bar() — self-consuming method returns a new type.
// Foo is consumed by into_bar, Bar is returned. The intermediate Foo is dropped
// inside the method. Result: bar.inner == 42.

struct Foo {
    val: i32
}

struct Bar {
    inner: i32
}

impl Foo {
    fn into_bar(self: Self) -> Bar {
        make Bar { inner: self.val }
    }
}

fn main() -> i32 {
    let bar = make Foo { val: 42 }.into_bar();
    bar.inner
}
