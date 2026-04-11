struct Pair { x: i32, y: i32 }
impl Copy for Pair {}

fn main() -> i32 {
    let p = make Pair { x: 10, y: 20 };
    let rp: *mut Pair = &raw mut p;
    *rp = make Pair { x: 20, y: 22 };
    p.x + p.y
}
