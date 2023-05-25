namespace Algorithms.DataStructures;

public struct Segment
{
    public int SegmentStartPosition;
    public int SegmentEndPosition;

    public Segment(int segmentStartPosition, int segmentLength)
    {
        SegmentStartPosition = segmentStartPosition;
        SegmentEndPosition = segmentStartPosition + segmentLength;
    }
}