// bruh.dd where bruh: &Foo and dd: Gc(i32) is non-Copy.
// Cannot move a non-Copy field out through a reference.

struct Foo { dd: Gc(i32) }

fn main() -> i32 {
    let made = make Foo { dd: Gc.New(4) };
    let bruh = &made;
    let idk = bruh.dd;
    6
}
