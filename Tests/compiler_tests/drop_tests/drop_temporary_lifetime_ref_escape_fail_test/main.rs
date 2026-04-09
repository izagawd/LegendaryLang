// make Foo { val: 42 }.get_ref() inside an inner block, returned out.
// The temporary Foo is spilled to the inner block scope. When the block exits,
// the temporary is dropped, making the reference dangle. Must be rejected.

struct Foo {
    val: i32
}

impl Foo {
    fn get_ref(self: &Self) -> &i32 {
        &self.val
    }
}

fn main() -> i32 {
    let r: &i32 = {
        make Foo { val: 42 }.get_ref()
    };
    *r
}
