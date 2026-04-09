// bruh.val where bruh: &Foo and val: i32 is Copy. This is fine.

struct Foo { val: i32 }

fn main() -> i32 {
    let made = make Foo { val: 42 };
    let bruh = &made;
    bruh.val
}
