struct Holder['a] {
    val: &'a uniq i32
}

fn use_holder(h: &Holder) -> i32 { *h.val }

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &uniq x };
    let val = use_holder(&h);
    x + val
}
