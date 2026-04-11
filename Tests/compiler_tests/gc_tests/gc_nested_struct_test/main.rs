struct Inner {
    val: i32
}
impl Copy for Inner {}

struct Outer {
    a: Inner,
    b: Inner
}
impl Copy for Outer {}

fn main() -> i32 {
    let b: GcMut(Outer) = GcMut.New(make Outer {
        a: make Inner { val: 10 },
        b: make Inner { val: 32 }
    });
    (*b).a.val + (*b).b.val
}
