use Std.Deref.Deref;
use Std.Ptr.GetMetadata;

fn check_len(r: &str) -> usize {
    let rp: *shared str = &raw *r;
    GetMetadata(rp)
}

fn main() -> i32 {
    let a = "hi";
    let b = "hello";
    let len_a: usize = check_len(a.deref());
    let len_b: usize = check_len(b.deref());
    if len_a == 2 {
        if len_b == 5 {
            1
        } else {
            0
        }
    } else {
        0
    }
}
