// Two separate temporaries, each returning a &i32 via get_ref().
// Both are spilled independently. Both refs valid in the same scope.
// Result: 20 + 22 = 42.

struct Foo {
    val: i32
}

impl Foo {
    fn get_ref(self: &Self) -> &i32 {
        &self.val
    }
}

fn main() -> i32 {
    let a: &i32 = make Foo { val: 20 }.get_ref();
    let b: &i32 = make Foo { val: 22 }.get_ref();
    *a + *b
}
