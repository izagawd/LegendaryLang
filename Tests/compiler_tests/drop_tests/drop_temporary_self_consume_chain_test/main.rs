// Chain of Self-consuming method calls on temporaries.
// Each method takes ownership, creates a new Foo, passes it on.
// double: 10 → 20, add(22): 20 → 42, get_val: 42. Result: 42.

struct Foo {
    val: i32
}

impl Foo {
    fn double(self: Self) -> Foo {
        make Foo { val: self.val + self.val }
    }

    fn add(self: Self, n: i32) -> Foo {
        make Foo { val: self.val + n }
    }

    fn get_val(self: Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let a = make Foo { val: 10 }.double();
    let b = a.add(22);
    b.get_val()
}
