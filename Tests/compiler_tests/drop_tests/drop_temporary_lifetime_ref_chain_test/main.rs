// make Foo { val: 42 }.as_wrapper() returns Wrapper['a] { inner: &'a i32 }.
// The temporary Foo is spilled because return type has lifetime dependency.
// We then access wrapper.inner to read the value. Result: 42.

struct Foo {
    val: i32
}

struct Wrapper['a] {
    inner: &'a i32
}

impl Foo {
    fn as_wrapper(self: &Self) -> Wrapper {
        make Wrapper { inner: &self.val }
    }
}

fn main() -> i32 {
    let w = make Foo { val: 42 }.as_wrapper();
    *w.inner
}
