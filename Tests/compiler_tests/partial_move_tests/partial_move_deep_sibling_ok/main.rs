struct Inner { val: i32 }
struct Outer { a: Inner, b: Inner }
struct Deep { x: Outer, y: Inner }
fn consume(x: Inner) -> i32 { x.val }
fn main() -> i32 {
    let s = make Deep { x: make Outer { a: make Inner { val: 20 }, b: make Inner { val: 22 } }, y: make Inner { val: 0 } };
    let moved = s.x.a;
    consume(s.x.b)
}
