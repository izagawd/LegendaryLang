struct Leaf { val: i32 }
struct L3 { c: Leaf, d: Leaf }
struct L2 { b: L3 }
struct L1 { a: L2, e: Leaf }
struct Root { r: L1, s: Leaf }
fn main() -> i32 {
    let r = make Root {
        r: make L1 { a: make L2 { b: make L3 { c: make Leaf { val: 0 }, d: make Leaf { val: 0 } } }, e: make Leaf { val: 0 } },
        s: make Leaf { val: 0 }
    };
    let moved = r.r.a.b.c;
    let parent = r.r.a.b;
    42
}
