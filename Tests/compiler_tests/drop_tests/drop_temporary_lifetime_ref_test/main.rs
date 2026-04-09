// make Foo { val: 42 }.get_ref() returns &i32 tied to &self's lifetime.
// The temporary Foo is spilled to an anonymous local in the current scope.
// The reference is valid within this scope. Result: 42.

struct Foo {
    val: i32
}

impl Foo {
    fn get_ref(self: &Self) -> &i32 {
        &self.val
    }
}

fn main() -> i32 {
    let r: &i32 = make Foo { val: 42 }.get_ref();
    *r
}
