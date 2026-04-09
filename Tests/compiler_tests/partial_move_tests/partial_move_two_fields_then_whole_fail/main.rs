struct Inner { val: i32 }
struct Outer { a: Inner, b: Inner }
struct Deep { x: Outer, y: Inner }
fn consume_outer(x: Outer) -> i32 { x.a.val + x.b.val }
fn main() -> i32 {
    let s = make Outer { a: make Inner { val: 0 }, b: make Inner { val: 0 } };
    let a = s.a;
    let b = s.b;
    consume_outer(s)
}
