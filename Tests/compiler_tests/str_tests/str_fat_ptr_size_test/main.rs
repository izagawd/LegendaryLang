use Std.Mem.SizeOf;

fn main() -> i32 {
    let fat: usize = SizeOf(&const str);
    let thin: usize = SizeOf(usize);
    if fat == thin + thin {
        1
    } else {
        0
    }
}
